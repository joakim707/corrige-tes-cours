using CorrigeTesCours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrigeTesCours.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasMaxLength(255).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(u => u.Pseudo).HasMaxLength(50).IsRequired();
        b.Property(u => u.Level).HasConversion<string>().HasMaxLength(20);
    }
}

public class MatiereConfiguration : IEntityTypeConfiguration<Matiere>
{
    public void Configure(EntityTypeBuilder<Matiere> b)
    {
        b.HasKey(m => m.Id);
        b.Property(m => m.Nom).HasMaxLength(100).IsRequired();
        b.Property(m => m.Couleur).HasMaxLength(7).IsRequired();
        b.Property(m => m.Niveau).HasConversion<string>().HasMaxLength(20);

        b.HasOne(m => m.User)
            .WithMany(u => u.Matieres)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Une matière porte le même nom au plus une fois par utilisateur.
        b.HasIndex(m => new { m.UserId, m.Nom }).IsUnique();
    }
}

public class CorrectionConfiguration : IEntityTypeConfiguration<Correction>
{
    public void Configure(EntityTypeBuilder<Correction> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.ContenuInput).IsRequired();
        b.Property(c => c.ContenuIA).IsRequired();

        b.HasOne(c => c.User)
            .WithMany(u => u.Corrections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Supprimer une matière ne doit pas effacer l'historique de corrections.
        b.HasOne(c => c.Matiere)
            .WithMany(m => m.Corrections)
            .HasForeignKey(c => c.MatiereId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(c => new { c.UserId, c.CreatedAt });
    }
}

public class FicheConfiguration : IEntityTypeConfiguration<Fiche>
{
    public void Configure(EntityTypeBuilder<Fiche> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Titre).HasMaxLength(200).IsRequired();
        b.Property(f => f.Resume).IsRequired();
        b.Property(f => f.PointsCles).HasColumnType("jsonb");
        b.Property(f => f.Definitions).HasColumnType("jsonb");
        b.Property(f => f.Formules).HasColumnType("jsonb");

        b.HasOne(f => f.User)
            .WithMany(u => u.Fiches)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(f => f.Matiere)
            .WithMany(m => m.Fiches)
            .HasForeignKey(f => f.MatiereId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(f => new { f.UserId, f.CreatedAt });
    }
}

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> b)
    {
        b.HasKey(q => q.Id);
        b.Property(q => q.Titre).HasMaxLength(200).IsRequired();
        b.Property(q => q.Questions).HasColumnType("jsonb");

        b.HasOne(q => q.User)
            .WithMany(u => u.Quizzes)
            .HasForeignKey(q => q.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Le quiz survit à la suppression de la fiche dont il est issu.
        b.HasOne(q => q.Fiche)
            .WithMany(f => f.Quizzes)
            .HasForeignKey(q => q.FicheId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(q => q.Matiere)
            .WithMany(m => m.Quizzes)
            .HasForeignKey(q => q.MatiereId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class QuizResultConfiguration : IEntityTypeConfiguration<QuizResult>
{
    public void Configure(EntityTypeBuilder<QuizResult> b)
    {
        b.HasKey(r => r.Id);
        b.Property(r => r.Details).HasColumnType("jsonb");

        b.HasOne(r => r.Quiz)
            .WithMany(q => q.Results)
            .HasForeignKey(r => r.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade côté User ferait deux chemins de suppression vers QuizResult.
        b.HasOne(r => r.User)
            .WithMany(u => u.QuizResults)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(r => new { r.UserId, r.PassedAt });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        b.HasIndex(t => t.TokenHash).IsUnique();
        b.Ignore(t => t.IsActive);

        b.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
