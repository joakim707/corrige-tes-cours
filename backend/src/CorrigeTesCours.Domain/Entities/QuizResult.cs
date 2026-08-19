namespace CorrigeTesCours.Domain.Entities;

public class QuizAnswerDetail
{
    public int QuestionIndex { get; set; }
    public string ReponseUtilisateur { get; set; } = null!;
    public bool Correcte { get; set; }
    public string Explication { get; set; } = null!;
}

public class QuizResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Score sur 100.</summary>
    public int Score { get; set; }
    /// <summary>Détail réponse par réponse, stocké en jsonb.</summary>
    public List<QuizAnswerDetail> Details { get; set; } = new();
    public DateTime PassedAt { get; set; } = DateTime.UtcNow;

    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
