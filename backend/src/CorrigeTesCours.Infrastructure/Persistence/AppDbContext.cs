using CorrigeTesCours.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Matiere> Matieres => Set<Matiere>();
    public DbSet<Correction> Corrections => Set<Correction>();
    public DbSet<Fiche> Fiches => Set<Fiche>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
