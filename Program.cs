using FootballPredictor.Web.Data;
using Microsoft.EntityFrameworkCore;
using FootballPredictor.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=football.db"));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<PredictionService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<BacktestService>();

// Регистрация HttpClient для FootballDataOrgClient
builder.Services.AddHttpClient<FootballDataOrgClient>();

// Регистрация сервиса импорта
builder.Services.AddScoped<FootballDataOrgImportService>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();  // применяет миграции
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
