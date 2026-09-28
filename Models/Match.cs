namespace FootballPredictor.Web.Models;

public class Match
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }
    public int AwayTeamId { get; set; }
    public Team? AwayTeam { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = "Scheduled";

    // Статистика матча (из CSV)
    public int? HomeShots { get; set; }
    public int? AwayShots { get; set; }
    public int? HomeShotsOnTarget { get; set; }
    public int? AwayShotsOnTarget { get; set; }
    public int? HomeCorners { get; set; }
    public int? AwayCorners { get; set; }

    // Рыночные коэффициенты (Bet365)
    public decimal? HomeOdds { get; set; }
    public decimal? DrawOdds { get; set; }
    public decimal? AwayOdds { get; set; }
    public string? ExternalId { get; set; }
}