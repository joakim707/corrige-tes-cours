namespace CorrigeTesCours.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Hash SHA-256 du token : le token brut n'est jamais persisté.</summary>
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Renseigné à la rotation : l'ancien token devient inutilisable.</summary>
    public DateTime? RevokedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
