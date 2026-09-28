using CsvHelper;
using FootballPredictor.Web.Data;
using FootballPredictor.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FootballPredictor.Web.Services;

public class CsvImportService
{
    private readonly AppDbContext _context;

    public CsvImportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ImportResult> ImportMatchesAsync(string filePath)
    {
        var result = new ImportResult();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Читаем заголовки
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (await csv.ReadAsync())
        {
            // Читаем строку как словарь (устойчиво к разному набору колонок)
            var row = new Dictionary<string, string>();
            foreach (var h in headers)
            {
                row[h] = csv.GetField(h) ?? string.Empty;
            }

            var homeName = row.GetValueOrDefault("HomeTeam", "").Trim();
            var awayName = row.GetValueOrDefault("AwayTeam", "").Trim();
            if (string.IsNullOrEmpty(homeName) || string.IsNullOrEmpty(awayName))
                continue;

            // Дата: пробуем разные форматы
            var dateStr = row.GetValueOrDefault("Date", "");
            if (!TryParseDate(dateStr, out var date))
            {
                result.Skipped++;
                continue;
            }

            // Голы
            if (!TryParseInt(row.GetValueOrDefault("FTHG"), out var homeGoals) ||
                !TryParseInt(row.GetValueOrDefault("FTAG"), out var awayGoals))
            {
                result.Skipped++;
                continue;
            }

            // Найти или создать команды
            var homeTeam = await GetOrCreateTeamAsync(homeName);
            var awayTeam = await GetOrCreateTeamAsync(awayName);

            // Проверка на дубликат: та же пара команд и та же дата
            bool exists = await _context.Matches.AnyAsync(m =>
                m.HomeTeamId == homeTeam.Id &&
                m.AwayTeamId == awayTeam.Id &&
                m.StartTimeUtc == date);

            if (exists)
            {
                result.Skipped++;
                continue;
            }

            var match = new Match
            {
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                StartTimeUtc = date,
                Status = "Finished",
                HomeScore = homeGoals,
                AwayScore = awayGoals,
                HomeShots = ParseNullableInt(row.GetValueOrDefault("HS")),
                AwayShots = ParseNullableInt(row.GetValueOrDefault("AS")),
                HomeShotsOnTarget = ParseNullableInt(row.GetValueOrDefault("HST")),
                AwayShotsOnTarget = ParseNullableInt(row.GetValueOrDefault("AST")),
                HomeCorners = ParseNullableInt(row.GetValueOrDefault("HC")),
                AwayCorners = ParseNullableInt(row.GetValueOrDefault("AC")),
                HomeOdds = ParseNullableDecimal(row.GetValueOrDefault("B365H")),
                DrawOdds = ParseNullableDecimal(row.GetValueOrDefault("B365D")),
                AwayOdds = ParseNullableDecimal(row.GetValueOrDefault("B365A")),
            };

            _context.Matches.Add(match);
            result.Imported++;
        }

        await _context.SaveChangesAsync();
        return result;
    }

    private async Task<Team> GetOrCreateTeamAsync(string name)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == name);
        if (team != null) return team;

        team = new Team { Name = name, League = "EPL" };
        _context.Teams.Add(team);
        await _context.SaveChangesAsync(); // получаем Id
        return team;
    }

    private static bool TryParseDate(string s, out DateTime date)
    {
        // Football-Data может использовать разные форматы
        var formats = new[] { "dd/MM/yy", "dd/MM/yyyy", "yyyy-MM-dd" };
        foreach (var f in formats)
        {
            if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out date))
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                return true;
            }
        }
        date = default;
        return false;
    }

    private static bool TryParseInt(string? s, out int value)
    {
        value = 0;
        return !string.IsNullOrWhiteSpace(s) && int.TryParse(s.Trim(), out value);
    }

    private static int? ParseNullableInt(string? s)
    {
        return TryParseInt(s, out var v) ? v : null;
    }

    private static decimal? ParseNullableDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            ? v : null;
    }
}

public class ImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
}