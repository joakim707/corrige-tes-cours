using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Dtos;

public record SubmitCorrectionRequest(
    [Required, MinLength(3), MaxLength(8000)] string Exercice,
    Guid? MatiereId,
    /// <summary>true = correction complète directe ; false (défaut) = indices puis méthode, sans donner la réponse.</summary>
    bool DemanderCorrectionComplete = false);

public record CorrectionResponse(Guid Id, string ContenuInput, string ContenuIA, Guid? MatiereId, DateTime CreatedAt)
{
    public static CorrectionResponse From(Correction c) => new(c.Id, c.ContenuInput, c.ContenuIA, c.MatiereId, c.CreatedAt);
}

/// <summary>Forme JSON attendue de l'IA pour une réponse pédagogique.</summary>
public class AiCorrectionPayload
{
    [JsonPropertyName("matiereDetectee")]
    public string MatiereDetectee { get; set; } = "";

    [JsonPropertyName("reponse")]
    public string Reponse { get; set; } = "";
}
