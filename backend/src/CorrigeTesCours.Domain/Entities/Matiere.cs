namespace CorrigeTesCours.Domain.Entities;

public class Matiere
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = null!;
    /// <summary>Couleur hexadécimale d'affichage, ex. "#4F46E5".</summary>
    public string Couleur { get; set; } = "#4F46E5";
    public NiveauScolaire Niveau { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Fiche> Fiches { get; set; } = new List<Fiche>();
    public ICollection<Correction> Corrections { get; set; } = new List<Correction>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
