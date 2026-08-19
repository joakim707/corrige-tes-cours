using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Dtos;

public record GenerateQuizRequest(
    Guid? FicheId,
    [MaxLength(20000)] string? Sujet,
    Guid? MatiereId,
    [Range(3, 20)] int NombreQuestions = 5);

public record QuizSummaryResponse(Guid Id, string Titre, int NombreQuestions, Guid? FicheId, Guid? MatiereId, DateTime CreatedAt)
{
    public static QuizSummaryResponse From(Quiz q) => new(q.Id, q.Titre, q.Questions.Count, q.FicheId, q.MatiereId, q.CreatedAt);
}

/// <summary>Question exposée avant soumission : sans réponse attendue ni explication.</summary>
public record QuizQuestionPlay(int Index, string Enonce, QuestionType Type, List<string> Options);

public record QuizPlayResponse(Guid Id, string Titre, List<QuizQuestionPlay> Questions)
{
    public static QuizPlayResponse From(Quiz q) => new(
        q.Id,
        q.Titre,
        q.Questions.Select((question, i) => new QuizQuestionPlay(i, question.Enonce, question.Type, question.Options)).ToList());
}

public record QuizAnswerSubmission(int QuestionIndex, string Reponse);

public record SubmitQuizRequest([Required] List<QuizAnswerSubmission> Reponses);

public record QuizResultResponse(Guid Id, Guid QuizId, int Score, List<QuizAnswerDetail> Details, DateTime PassedAt)
{
    public static QuizResultResponse From(QuizResult r) => new(r.Id, r.QuizId, r.Score, r.Details, r.PassedAt);
}

public record QuizResultSummaryResponse(Guid Id, Guid QuizId, string QuizTitre, int Score, DateTime PassedAt);

/// <summary>Forme JSON attendue de l'IA pour un quiz généré.</summary>
public class AiQuizPayload
{
    [JsonPropertyName("titre")]
    public string Titre { get; set; } = "";

    [JsonPropertyName("questions")]
    public List<AiQuizQuestion> Questions { get; set; } = new();
}

public class AiQuizQuestion
{
    [JsonPropertyName("enonce")]
    public string Enonce { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Qcm";

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    [JsonPropertyName("reponseAttendue")]
    public string ReponseAttendue { get; set; } = "";

    [JsonPropertyName("explication")]
    public string Explication { get; set; } = "";
}
