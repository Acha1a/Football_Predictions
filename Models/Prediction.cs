namespace FootballPredictor.Web.Models;

public class Prediction
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match? Match { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public decimal HomeProbability { get; set; }
    public decimal DrawProbability { get; set; }
    public decimal AwayProbability { get; set; }

    public int Confidence { get; set; }
    public string RiskLevel { get; set; } = "Medium";
    public string PredictedOutcome { get; set; } = "";

    public string? ActualOutcome { get; set; }
    public bool? IsCorrect { get; set; }

    public List<PredictionFactor> Factors { get; set; } = new();
}