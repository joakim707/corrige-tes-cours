using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Dtos;

public record GenerateFicheRequest(
    [Required, MinLength(20), MaxLength(20000)] string Cours,
    Guid? MatiereId);

public record FicheResponse(
    Guid Id,
    string Titre,
    string Resume,
    List<string> PointsCles,
    Dictionary<string, string> Definitions,
    List<string> Formules,
    Guid? MatiereId,
    DateTime CreatedAt)
{
    public static FicheResponse From(Fiche f) =>
        new(f.Id, f.Titre, f.Resume, f.PointsCles, f.Definitions, f.Formules, f.MatiereId, f.CreatedAt);
}

public record FicheSummaryResponse(Guid Id, string Titre, string Resume, Guid? MatiereId, DateTime CreatedAt)
{
    public static FicheSummaryResponse From(Fiche f) => new(f.Id, f.Titre, f.Resume, f.MatiereId, f.CreatedAt);
}

/// <summary>Forme JSON attendue de l'IA pour une fiche de révision.</summary>
public class AiFichePayload
{
    [JsonPropertyName("titre")]
    public string Titre { get; set; } = "";

    [JsonPropertyName("resume")]
    public string Resume { get; set; } = "";

    [JsonPropertyName("pointsCles")]
    public List<string> PointsCles { get; set; } = new();

    [JsonPropertyName("definitions")]
    public Dictionary<string, string> Definitions { get; set; } = new();

    [JsonPropertyName("formules")]
    public List<string> Formules { get; set; } = new();
}
