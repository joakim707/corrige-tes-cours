namespace CorrigeTesCours.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Pseudo { get; set; } = null!;
    public NiveauScolaire Level { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Matiere> Matieres { get; set; } = new List<Matiere>();
    public ICollection<Correction> Corrections { get; set; } = new List<Correction>();
    public ICollection<Fiche> Fiches { get; set; } = new List<Fiche>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
