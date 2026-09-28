using FootballPredictor.Web.Data;
using FootballPredictor.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictor.Web.Services;

public class FootballDataOrgImportService
{
    private readonly AppDbContext _context;
    private readonly FootballDataOrgClient _client;

    public FootballDataOrgImportService(AppDbContext context, FootballDataOrgClient client)
    {
        _context = context;
        _client = client;
    }

    public async Task<ImportResult> ImportUpcomingMatchesAsync(string competitionCode = "PL")
{
    var result = new ImportResult();
    var matches = await _client.GetUpcomingMatchesAsync(competitionCode);

    foreach (var match in matches)
    {
        if (string.IsNullOrWhiteSpace(match.HomeTeam?.Name) ||
            string.IsNullOrWhiteSpace(match.AwayTeam?.Name))
        {
            result.Skipped++;
            continue;
        }

        // Нормализуем имена и находим/создаём команды
        var homeTeam = await GetOrCreateTeamAsync(match.HomeTeam.Name);
        var awayTeam = await GetOrCreateTeamAsync(match.AwayTeam.Name);

        // Дедупликация по ExternalId
        var externalId = match.Id.ToString();
        if (await _context.Matches.AnyAsync(m => m.ExternalId == externalId))
        {
            result.Skipped++;
            continue;
        }

        // Дедупликация по паре команд + дате
        var matchDate = match.UtcDate.Date;
        bool samePairSameDay = await _context.Matches.AnyAsync(m =>
            m.HomeTeamId == homeTeam.Id &&
            m.AwayTeamId == awayTeam.Id &&
            m.StartTimeUtc.Date == matchDate);

        if (samePairSameDay)
        {
            result.Skipped++;
            continue;
        }

        // Добавляем матч
        _context.Matches.Add(new Match
        {
            ExternalId = externalId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            StartTimeUtc = match.UtcDate,
            Status = "Scheduled"
        });

        result.Imported++;
    }

    await _context.SaveChangesAsync();
    return result;
}

private async Task<Team> GetOrCreateTeamAsync(string rawName)
{
    var name = NormalizeTeamName(rawName);

    var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == name);
    if (team != null) return team;

    team = new Team { Name = name, League = "PL" };
    _context.Teams.Add(team);
    await _context.SaveChangesAsync(); // нужно, чтобы получить Id
    return team;
}


    /// <summary>
    /// Приводит имена команд из Football-Data.org к тем, что используются в Football-Data CSV.
    /// </summary>
    private static string NormalizeTeamName(string name)
    {
        name = name.Trim();

        // Убираем типовые суффиксы
        name = name
            .Replace(" FC", "")
            .Replace(" AFC", "")
            .Replace(" & Hove Albion", "")
            .Replace(" Hotspur", "")
            .Replace("United", "United") // на случай разных вариантов
            .Trim();

        // Специальные переименования
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Nott'm Forest", "Nott'm Forest" },
            { "Nottingham Forest", "Nott'm Forest" },
            { "Man United", "Man United" },
            { "Manchester United", "Man United" },
            { "Man City", "Man City" },
            { "Manchester City", "Man City" },
            { "Brighton", "Brighton" },
            { "Sunderland", "Sunderland" },
            { "Ipswich Town", "Ipswich" },
            { "Hull City", "Hull" },
            { "Coventry City", "Coventry" },
            { "Leeds United", "Leeds" },
            { "AFC Bournemouth", "Bournemouth" },
            { "Bournemouth", "Bournemouth" },
            { "West Ham", "West Ham" },
            { "Newcastle", "Newcastle" },
            { "Newcastle United", "Newcastle" },
            { "Newcastle United FC", "Newcastle" },
        };

        if (mapping.TryGetValue(name, out var mapped))
            return mapped;

        return name;
    }
}