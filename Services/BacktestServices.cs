using FootballPredictor.Web.Data;
using FootballPredictor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictor.Web.Services;

public class BacktestService
{
    private readonly AppDbContext _context;
    private readonly PredictionService _predictionService;

    public BacktestService(AppDbContext context, PredictionService predictionService)
    {
        _context = context;
        _predictionService = predictionService;
    }

    public async Task<BacktestResult> RunAsync(DateTime from, DateTime to)
    {
        // Загружаем ВСЕ завершённые матчи (нужны как история)
        var allFinished = await _context.Matches
            .Where(m => m.Status == "Finished" && m.HomeScore != null && m.AwayScore != null)
            .OrderBy(m => m.StartTimeUtc)
            .ToListAsync();

        // Какие матчи бэктестим
        var toPredict = allFinished
            .Where(m => m.StartTimeUtc >= from && m.StartTimeUtc <= to)
            .ToList();

        // Какие из них уже имеют прогноз — пропускаем, чтобы не дублировать
        var existingMatchIds = (await _context.Predictions
            .Select(p => p.MatchId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        int created = 0, skipped = 0, correct = 0, total = 0;

        foreach (var match in toPredict)
        {
            if (existingMatchIds.Contains(match.Id))
            {
                skipped++;
                continue;
            }

            // История команд: только матчи ДО даты прогнозируемого матча
            var homeHistory = allFinished
                .Where(m => (m.HomeTeamId == match.HomeTeamId || m.AwayTeamId == match.HomeTeamId)
                            && m.StartTimeUtc < match.StartTimeUtc)
                .OrderByDescending(m => m.StartTimeUtc)
                .Take(5)
                .ToList();

            var awayHistory = allFinished
                .Where(m => (m.HomeTeamId == match.AwayTeamId || m.AwayTeamId == match.AwayTeamId)
                            && m.StartTimeUtc < match.StartTimeUtc)
                .OrderByDescending(m => m.StartTimeUtc)
                .Take(5)
                .ToList();

            // Мало истории — пропускаем (первые туры сезона)
            if (homeHistory.Count < 3 || awayHistory.Count < 3)
            {
                skipped++;
                continue;
            }

            var prediction = _predictionService.BuildPrediction(match, homeHistory, awayHistory);
            if (prediction == null) { skipped++; continue; }

            // Эмулируем «прогноз сделан за день до матча»
            prediction.CreatedAtUtc = match.StartTimeUtc.AddDays(-1);

            // Фактический исход
            string actual = match.HomeScore > match.AwayScore ? "HomeWin"
                          : match.HomeScore < match.AwayScore ? "AwayWin"
                          : "Draw";
            prediction.ActualOutcome = actual;
            prediction.IsCorrect = prediction.PredictedOutcome == actual;

            _context.Predictions.Add(prediction);

            created++;
            total++;
            if (prediction.IsCorrect == true) correct++;
        }

        await _context.SaveChangesAsync();

        return new BacktestResult
        {
            Created = created,
            Skipped = skipped,
            Correct = correct,
            Total = total
        };
    }
}

public class BacktestResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
}