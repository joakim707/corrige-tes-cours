namespace CorrigeTesCours.Domain.Entities;

public class Fiche
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titre { get; set; } = null!;
    public string Resume { get; set; } = null!;
    /// <summary>Liste de points clés, stockée en jsonb.</summary>
    public List<string> PointsCles { get; set; } = new();
    /// <summary>Définitions terme -> explication, stockées en jsonb.</summary>
    public Dictionary<string, string> Definitions { get; set; } = new();
    /// <summary>Formules importantes, stockées en jsonb.</summary>
    public List<string> Formules { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? MatiereId { get; set; }
    public Matiere? Matiere { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
