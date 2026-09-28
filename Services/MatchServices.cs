using FootballPredictor.Web.Data;

namespace FootballPredictor.Web.Services;

public class MatchService
{
    private readonly AppDbContext _context;
    private readonly PredictionService _predictionService;

    public MatchService(AppDbContext context, PredictionService predictionService)
    {
        _context = context;
        _predictionService = predictionService;
    }

    public void UpdateMatchResult(int matchId, int homeScore, int awayScore)
    {
        var match = _context.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null) return;

        // Обновляем матч
        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        match.Status = "Finished";
        _context.SaveChanges();

        // Если прогноза для матча ещё нет — генерируем его
        bool hasPredictions = _context.Predictions.Any(p => p.MatchId == matchId);
        if (!hasPredictions)
        {
            _predictionService.PredictMatch(matchId);
        }

        // Определяем фактический исход
        string actualOutcome = homeScore > awayScore ? "HomeWin"
                              : homeScore < awayScore ? "AwayWin"
                              : "Draw";

        // Обновляем все прогнозы для этого матча
        var predictions = _context.Predictions
            .Where(p => p.MatchId == matchId)
            .ToList();

        foreach (var pred in predictions)
        {
            pred.ActualOutcome = actualOutcome;
            pred.IsCorrect = pred.PredictedOutcome == actualOutcome;
        }

        _context.SaveChanges();
    }
}