using CorrigeTesCours.Api.Ai;
using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Domain.Entities;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/corrections")]
[Authorize]
public class CorrectionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiClient _ai;

    public CorrectionsController(AppDbContext db, IAiClient ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost]
    public async Task<ActionResult<CorrectionResponse>> Submit(SubmitCorrectionRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (request.MatiereId is { } matiereId)
        {
            var owns = await _db.Matieres.AnyAsync(m => m.Id == matiereId && m.UserId == userId, ct);
            if (!owns) return NotFound(new ProblemDetails { Title = "Matière introuvable", Status = 404 });
        }

        const string systemPrompt = """
            Tu es un tuteur pédagogique pour élèves du collège au supérieur. On te soumet un exercice.
            Ne donne JAMAIS la réponse finale directement : donne des indices, la méthode à suivre, puis
            une correction complète UNIQUEMENT si demandeCorrectionComplete est true dans la requête.
            Adapte le niveau de langage à la matière détectée. Réponds en français.
            Réponds strictement en JSON avec les clés "matiereDetectee" (string, ex: "Mathématiques")
            et "reponse" (string, en Markdown, contenant les indices/méthode/correction).
            """;

        var userPrompt = $"""
            Exercice soumis :
            ---
            {request.Exercice}
            ---
            demandeCorrectionComplete = {request.DemanderCorrectionComplete.ToString().ToLowerInvariant()}
            """;

        AiCorrectionPayload payload;
        try
        {
            payload = await _ai.CompleteJsonAsync<AiCorrectionPayload>(systemPrompt, userPrompt, ct);
        }
        catch (AiUnavailableException ex)
        {
            return StatusCode(502, new ProblemDetails { Title = "Service IA indisponible", Detail = ex.Message, Status = 502 });
        }

        var correction = new Correction
        {
            ContenuInput = request.Exercice,
            ContenuIA = payload.Reponse,
            MatiereId = request.MatiereId,
            UserId = userId
        };

        _db.Corrections.Add(correction);
        await _db.SaveChangesAsync(ct);

        return Ok(CorrectionResponse.From(correction));
    }

    [HttpGet]
    public async Task<ActionResult<List<CorrectionResponse>>> GetAll([FromQuery] Guid? matiereId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var query = _db.Corrections.AsNoTracking().Where(c => c.UserId == userId);
        if (matiereId is { } m) query = query.Where(c => c.MatiereId == m);

        var corrections = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => CorrectionResponse.From(c))
            .ToListAsync(ct);

        return Ok(corrections);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CorrectionResponse>> GetOne(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var correction = await _db.Corrections.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        return correction is null ? NotFound() : Ok(CorrectionResponse.From(correction));
    }
}
