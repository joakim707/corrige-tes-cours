using System.ComponentModel.DataAnnotations;
using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Dtos;

public record CreateMatiereRequest(
    [Required, MinLength(1), MaxLength(100)] string Nom,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")] string Couleur,
    [Required] NiveauScolaire Niveau);

public record MatiereResponse(Guid Id, string Nom, string Couleur, NiveauScolaire Niveau, DateTime CreatedAt)
{
    public static MatiereResponse From(Matiere m) => new(m.Id, m.Nom, m.Couleur, m.Niveau, m.CreatedAt);
}
