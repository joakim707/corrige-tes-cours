namespace CorrigeTesCours.Domain.Entities;

public enum QuestionType
{
    Qcm = 0,
    VraiFaux = 1,
    Ouverte = 2
}

public class QuizQuestion
{
    public string Enonce { get; set; } = null!;
    public QuestionType Type { get; set; }
    /// <summary>Propositions pour un QCM ou un vrai/faux. Vide pour une question ouverte.</summary>
    public List<string> Options { get; set; } = new();
    public string ReponseAttendue { get; set; } = null!;
    public string Explication { get; set; } = null!;
}

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titre { get; set; } = null!;
    /// <summary>Questions générées par l'IA, stockées en jsonb.</summary>
    public List<QuizQuestion> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? FicheId { get; set; }
    public Fiche? Fiche { get; set; }

    public Guid? MatiereId { get; set; }
    public Matiere? Matiere { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
}
