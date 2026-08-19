using CorrigeTesCours.Api.Ai;
using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Api.Quizzes;
using CorrigeTesCours.Domain.Entities;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/quiz")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiClient _ai;

    public QuizController(AppDbContext db, IAiClient ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<QuizPlayResponse>> Generate(GenerateQuizRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        string sourceContent;
        string defaultTitre;
        Fiche? fiche = null;

        if (request.FicheId is { } ficheId)
        {
            fiche = await _db.Fiches.FirstOrDefaultAsync(f => f.Id == ficheId && f.UserId == userId, ct);
            if (fiche is null) return NotFound(new ProblemDetails { Title = "Fiche introuvable", Status = 404 });

            sourceContent = $"""
                Titre : {fiche.Titre}
                Résumé : {fiche.Resume}
                Points clés : {string.Join(" | ", fiche.PointsCles)}
                Définitions : {string.Join(" | ", fiche.Definitions.Select(d => $"{d.Key} = {d.Value}"))}
                Formules : {string.Join(" | ", fiche.Formules)}
                """;
            defaultTitre = $"Quiz — {fiche.Titre}";
        }
        else if (!string.IsNullOrWhiteSpace(request.Sujet))
        {
            sourceContent = request.Sujet;
            defaultTitre = "Quiz";
        }
        else
        {
            return BadRequest(new ProblemDetails { Title = "Contenu manquant", Detail = "Fournir soit ficheId, soit sujet.", Status = 400 });
        }

        if (request.MatiereId is { } matiereId)
        {
            var owns = await _db.Matieres.AnyAsync(m => m.Id == matiereId && m.UserId == userId, ct);
            if (!owns) return NotFound(new ProblemDetails { Title = "Matière introuvable", Status = 404 });
        }

        var systemPrompt = $"""
            Tu génères un quiz de {request.NombreQuestions} questions variées (QCM, vrai/faux, questions ouvertes)
            à partir du contenu fourni. Réponds en français, strictement en JSON avec les clés :
            "titre" (string) et "questions" (array de {request.NombreQuestions} objets ayant :
            "enonce" (string), "type" (une valeur EXACTE parmi "Qcm", "VraiFaux", "Ouverte"),
            "options" (array de string ; 3-5 propositions pour Qcm, ["Vrai","Faux"] pour VraiFaux, [] pour Ouverte),
            "reponseAttendue" (string, doit correspondre exactement à une des options pour Qcm/VraiFaux),
            "explication" (string, courte, justifie la bonne réponse)).
            """;

        var userPrompt = $"""
            Contenu source :
            ---
            {sourceContent}
            ---
            """;

        AiQuizPayload payload;
        try
        {
            payload = await _ai.CompleteJsonAsync<AiQuizPayload>(systemPrompt, userPrompt, ct);
        }
        catch (AiUnavailableException ex)
        {
            return StatusCode(502, new ProblemDetails { Title = "Service IA indisponible", Detail = ex.Message, Status = 502 });
        }

        if (payload.Questions.Count == 0)
            return StatusCode(502, new ProblemDetails { Title = "Le quiz généré est vide", Status = 502 });

        var quiz = new Quiz
        {
            Titre = string.IsNullOrWhiteSpace(payload.Titre) ? defaultTitre : payload.Titre,
            Questions = payload.Questions.Select(q => new QuizQuestion
            {
                Enonce = q.Enonce,
                Type = Enum.TryParse<QuestionType>(q.Type, ignoreCase: true, out var t) ? t : QuestionType.Qcm,
                Options = q.Options,
                ReponseAttendue = q.ReponseAttendue,
                Explication = q.Explication
            }).ToList(),
            FicheId = fiche?.Id,
            MatiereId = request.MatiereId,
            UserId = userId
        };

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync(ct);

        return Ok(QuizPlayResponse.From(quiz));
    }

    [HttpGet]
    public async Task<ActionResult<List<QuizSummaryResponse>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var quizzes = await _db.Quizzes
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);

        return Ok(quizzes.Select(QuizSummaryResponse.From).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuizPlayResponse>> GetOne(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var quiz = await _db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);
        return quiz is null ? NotFound() : Ok(QuizPlayResponse.From(quiz));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<QuizResultResponse>> Submit(Guid id, SubmitQuizRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);
        if (quiz is null) return NotFound();

        var answersByIndex = request.Reponses.ToDictionary(r => r.QuestionIndex, r => r.Reponse);
        var details = new List<QuizAnswerDetail>();

        for (var i = 0; i < quiz.Questions.Count; i++)
        {
            var question = quiz.Questions[i];
            var userAnswer = answersByIndex.GetValueOrDefault(i, "");
            var correct = QuizGrading.IsCorrect(question, userAnswer);

            details.Add(new QuizAnswerDetail
            {
                QuestionIndex = i,
                ReponseUtilisateur = userAnswer,
                Correcte = correct,
                Explication = question.Explication
            });
        }

        var score = quiz.Questions.Count == 0 ? 0 : (int)Math.Round(100.0 * details.Count(d => d.Correcte) / quiz.Questions.Count);

        var result = new QuizResult
        {
            QuizId = quiz.Id,
            UserId = userId,
            Score = score,
            Details = details
        };

        _db.QuizResults.Add(result);
        await _db.SaveChangesAsync(ct);

        return Ok(QuizResultResponse.From(result));
    }

    [HttpGet("results")]
    public async Task<ActionResult<List<QuizResultSummaryResponse>>> GetResults(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var results = await _db.QuizResults
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.PassedAt)
            .Join(_db.Quizzes, r => r.QuizId, q => q.Id, (r, q) => new QuizResultSummaryResponse(r.Id, r.QuizId, q.Titre, r.Score, r.PassedAt))
            .ToListAsync(ct);

        return Ok(results);
    }
}
