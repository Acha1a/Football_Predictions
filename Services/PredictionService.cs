using FootballPredictor.Web.Data;
using FootballPredictor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictor.Web.Services;

public class PredictionService
{
    private readonly AppDbContext _context;

    public PredictionService(AppDbContext context)
    {
        _context = context;
    }

    public Prediction? PredictMatch(int matchId)
{
    var match = _context.Matches
        .Include(m => m.HomeTeam)
        .Include(m => m.AwayTeam)
        .FirstOrDefault(m => m.Id == matchId);
    if (match == null) return null;

    var homeMatches = GetLastMatches(match.HomeTeamId, 5, match.StartTimeUtc);
    var awayMatches = GetLastMatches(match.AwayTeamId, 5, match.StartTimeUtc);

    var prediction = BuildPrediction(match, homeMatches, awayMatches);
    if (prediction == null) return null;

    _context.Predictions.Add(prediction);
    _context.SaveChanges();
    return prediction;
}

/// <summary>
/// Строит прогноз без сохранения в БД. Используется и в PredictMatch, и в BacktestService.
/// </summary>
public Prediction? BuildPrediction(Match match, List<Match> homeMatches, List<Match> awayMatches)
{
    // Требуем минимальную историю
    if (homeMatches.Count == 0 || awayMatches.Count == 0)
        return null;

    // Сырые значения факторов
    double homeForm = CalculateForm(match.HomeTeamId, homeMatches);
    double awayForm = CalculateForm(match.AwayTeamId, awayMatches);

    double homeAttack = GetAverageGoalsScored(match.HomeTeamId, homeMatches, isHome: true);
    double awayAttack = GetAverageGoalsScored(match.AwayTeamId, awayMatches, isHome: false);

    double homeDefense = GetAverageGoalsConceded(match.HomeTeamId, homeMatches, isHome: true);
    double awayDefense = GetAverageGoalsConceded(match.AwayTeamId, awayMatches, isHome: false);

    // Нормализуем
    double homeAttackScore = Clamp01(homeAttack / 3.0);
    double awayAttackScore = Clamp01(awayAttack / 3.0);
    double homeDefenseScore = Clamp01(1.0 - homeDefense / 3.0);
    double awayDefenseScore = Clamp01(1.0 - awayDefense / 3.0);

    // Веса
    double weightForm = 0.35, weightAttack = 0.20, weightDefense = 0.20, weightHome = 0.15;
    double totalOther = weightForm + weightAttack + weightDefense + weightHome;
    weightForm /= totalOther; weightAttack /= totalOther;
    weightDefense /= totalOther; weightHome /= totalOther;

    double homeAdvantageValue = 0.70;
    double awayAdvantageValue = 0.30;

    // Скоринги
    double homeScore = weightForm * homeForm + weightAttack * homeAttackScore
                     + weightDefense * homeDefenseScore + weightHome * homeAdvantageValue;

    double awayScore = weightForm * awayForm + weightAttack * awayAttackScore
                     + weightDefense * awayDefenseScore + weightHome * awayAdvantageValue;

    double drawScore = weightForm * (1 - Math.Abs(homeForm - awayForm)) * 0.5
                 + weightAttack * (1 - Math.Abs(homeAttackScore - awayAttackScore)) * 0.5
                 + weightDefense * (1 - Math.Abs(homeDefenseScore - awayDefenseScore)) * 0.5
                 + weightHome * 0.25;

    homeScore = Math.Max(0.01, homeScore);
    awayScore = Math.Max(0.01, awayScore);
    drawScore = Math.Max(0.01, drawScore);

    double total = homeScore + drawScore + awayScore;
    double homeProb = homeScore / total;
    double drawProb = drawScore / total;
    double awayProb = awayScore / total;

    var prediction = new Prediction
    {
        MatchId = match.Id,
        CreatedAtUtc = DateTime.UtcNow,
        HomeProbability = (decimal)homeProb,
        DrawProbability = (decimal)drawProb,
        AwayProbability = (decimal)awayProb,
        Confidence = CalculateConfidence(homeProb, drawProb, awayProb),
        RiskLevel = CalculateRiskLevel(homeProb, drawProb, awayProb),
        PredictedOutcome = DetermineOutcome(homeProb, drawProb, awayProb),
        Factors = new List<PredictionFactor>
        {
            new PredictionFactor
            {
                Name = "Recent Form", Value = (decimal)homeForm,
                Weight = (decimal)weightForm, Contribution = (decimal)(homeForm * weightForm),
                Explanation = $"Home form: {homeForm:P0} (last {homeMatches.Count})."
            },
            new PredictionFactor
            {
                Name = "Attack", Value = (decimal)homeAttackScore,
                Weight = (decimal)weightAttack, Contribution = (decimal)(homeAttackScore * weightAttack),
                Explanation = $"Avg scored: {homeAttack:F2}."
            },
            new PredictionFactor
            {
                Name = "Defense", Value = (decimal)homeDefenseScore,
                Weight = (decimal)weightDefense, Contribution = (decimal)(homeDefenseScore * weightDefense),
                Explanation = $"Avg conceded: {homeDefense:F2}."
            },
            new PredictionFactor
            {
                Name = "Home Advantage", Value = (decimal)homeAdvantageValue,
                Weight = (decimal)weightHome, Contribution = (decimal)(homeAdvantageValue * weightHome),
                Explanation = "Home field advantage."
            }
        }
    };

    return prediction;
}

    private static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));

    private List<Match> GetLastMatches(int teamId, int count, DateTime? beforeDate = null)
    {
        var query = _context.Matches
            .Where(m => (m.HomeTeamId == teamId || m.AwayTeamId == teamId) && m.Status == "Finished");

        if (beforeDate.HasValue)
            query = query.Where(m => m.StartTimeUtc < beforeDate.Value);

        return query
            .OrderByDescending(m => m.StartTimeUtc)
            .Take(count)
            .ToList();
    }

    private double CalculateForm(int teamId, List<Match> matches)
    {
        if (matches.Count == 0) return 0.5;
        double points = 0;
        foreach (var m in matches)
        {
            int scored, conceded;
            if (m.HomeTeamId == teamId) { scored = m.HomeScore ?? 0; conceded = m.AwayScore ?? 0; }
            else { scored = m.AwayScore ?? 0; conceded = m.HomeScore ?? 0; }
            if (scored > conceded) points += 3;
            else if (scored == conceded) points += 1;
        }
        return points / (matches.Count * 3);
    }

    private double GetAverageGoalsScored(int teamId, List<Match> matches, bool isHome)
    {
        double total = 0;
        int relevantCount = 0;

        foreach (var m in matches)
        {
            if (isHome && m.HomeTeamId == teamId)
            {
                total += m.HomeScore ?? 0;
                relevantCount++;
            }
            else if (!isHome && m.AwayTeamId == teamId)
            {
                total += m.AwayScore ?? 0;
                relevantCount++;
            }
        }

        // Если релевантных матчей нет — возвращаем средний гол в футболе
        return relevantCount > 0 ? total / relevantCount : 1.0;
    }

    private double GetAverageGoalsConceded(int teamId, List<Match> matches, bool isHome)
    {
        double total = 0;
        int relevantCount = 0;

        foreach (var m in matches)
        {
            if (isHome && m.HomeTeamId == teamId)
            {
                total += m.AwayScore ?? 0;
                relevantCount++;
            }
            else if (!isHome && m.AwayTeamId == teamId)
            {
                total += m.HomeScore ?? 0;
                relevantCount++;
            }
        }

        return relevantCount > 0 ? total / relevantCount : 1.0;
    }

    private int CalculateConfidence(double homeProb, double drawProb, double awayProb)
    {
        var probs = new[] { homeProb, drawProb, awayProb }
            .OrderByDescending(p => p)
            .ToArray();

        double maxProb = probs[0];
        double secondProb = probs[1];

        // Отрыв лидера от второго места (0..1) + бонус за абсолютную величину max
        double gap = maxProb - secondProb;
        double confidence = 50 * maxProb + 50 * gap;

        return Math.Clamp((int)Math.Round(confidence), 0, 100);
    }

    private string CalculateRiskLevel(double homeProb, double drawProb, double awayProb)
    {
        double maxProb = Math.Max(homeProb, Math.Max(drawProb, awayProb));
        if (maxProb > 0.65) return "Low";
        if (maxProb > 0.45) return "Medium";
        return "High";
    }

    private string DetermineOutcome(double homeProb, double drawProb, double awayProb)
    {
        if (homeProb > drawProb && homeProb > awayProb) return "HomeWin";
        if (awayProb > drawProb && awayProb > homeProb) return "AwayWin";
        return "Draw";
    }
}