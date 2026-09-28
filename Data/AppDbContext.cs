using Microsoft.EntityFrameworkCore;
using FootballPredictor.Web.Models;

namespace FootballPredictor.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    public DbSet<PredictionFactor> PredictionFactors => Set<PredictionFactor>();

}