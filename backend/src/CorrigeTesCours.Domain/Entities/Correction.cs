namespace CorrigeTesCours.Domain.Entities;

public class Correction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ContenuInput { get; set; } = null!;
    public string ContenuIA { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? MatiereId { get; set; }
    public Matiere? Matiere { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
