using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration; // Добавим для чтения ключа из конфигурации

namespace FootballPredictor.Web.Services;

public class FootballDataOrgClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Ключ будет прочитан из appsettings.json
    public FootballDataOrgClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["FootballDataOrg:ApiKey"] ?? throw new InvalidOperationException("API key not configured.");
        
        _httpClient.BaseAddress = new Uri("https://api.football-data.org/v4/");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _apiKey);
    }


   public async Task<List<FootballDataMatch>> GetUpcomingMatchesAsync(string competitionCode = "PL", int totalDays = 60)
{
        var allMatches = new List<FootballDataMatch>();
        var start = DateTime.UtcNow.Date;

        for (int offset = 0; offset < totalDays; offset += 10)
        {
            var dateFrom = start.AddDays(offset).ToString("yyyy-MM-dd");
            var dateTo = start.AddDays(offset + 9).ToString("yyyy-MM-dd");

            var url = $"matches?competitions={competitionCode}&dateFrom={dateFrom}&dateTo={dateTo}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // 429 — лимит запросов. Прерываем цикл, возвращаем что есть.
                break;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FootballDataResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Matches != null)
                allMatches.AddRange(result.Matches);

            // Небольшая пауза, чтобы не нарушить 10 запросов/минуту
            await Task.Delay(500);
        }

        return allMatches;
}

// DTO для ответа API
public class FootballDataResponse
{
    [JsonPropertyName("matches")]
    public List<FootballDataMatch>? Matches { get; set; }
}

public class FootballDataMatch
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("utcDate")]
    public DateTime UtcDate { get; set; } // API возвращает дату уже в UTC

    [JsonPropertyName("status")]
    public string? Status { get; set; } // "SCHEDULED"

    [JsonPropertyName("homeTeam")]
    public FootballDataTeam? HomeTeam { get; set; }

    [JsonPropertyName("awayTeam")]
    public FootballDataTeam? AwayTeam { get; set; }

    [JsonPropertyName("competition")]
    public FootballDataCompetition? Competition { get; set; }
}

public class FootballDataTeam
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class FootballDataCompetition
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

}