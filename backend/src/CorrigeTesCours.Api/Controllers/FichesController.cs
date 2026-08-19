using CorrigeTesCours.Api.Ai;
using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Api.Pdf;
using CorrigeTesCours.Domain.Entities;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/fiches")]
[Authorize]
public class FichesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiClient _ai;

    public FichesController(AppDbContext db, IAiClient ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<FicheResponse>> Generate(GenerateFicheRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        if (request.MatiereId is { } matiereId)
        {
            var owns = await _db.Matieres.AnyAsync(m => m.Id == matiereId && m.UserId == userId, ct);
            if (!owns) return NotFound(new ProblemDetails { Title = "Matière introuvable", Status = 404 });
        }

        const string systemPrompt = """
            Tu transformes un cours en fiche de révision structurée pour un élève. Réponds en français,
            strictement en JSON avec les clés :
            "titre" (string, court), "resume" (string, 3-5 phrases),
            "pointsCles" (array de string, 4-8 items), "definitions" (objet clé=terme / valeur=explication courte),
            "formules" (array de string ; tableau vide si la matière n'a pas de formules).
            """;

        var userPrompt = $"""
            Cours à transformer en fiche :
            ---
            {request.Cours}
            ---
            """;

        AiFichePayload payload;
        try
        {
            payload = await _ai.CompleteJsonAsync<AiFichePayload>(systemPrompt, userPrompt, ct);
        }
        catch (AiUnavailableException ex)
        {
            return StatusCode(502, new ProblemDetails { Title = "Service IA indisponible", Detail = ex.Message, Status = 502 });
        }

        var fiche = new Fiche
        {
            Titre = string.IsNullOrWhiteSpace(payload.Titre) ? "Fiche sans titre" : payload.Titre,
            Resume = payload.Resume,
            PointsCles = payload.PointsCles,
            Definitions = payload.Definitions,
            Formules = payload.Formules,
            MatiereId = request.MatiereId,
            UserId = userId
        };

        _db.Fiches.Add(fiche);
        await _db.SaveChangesAsync(ct);

        return Ok(FicheResponse.From(fiche));
    }

    [HttpGet]
    public async Task<ActionResult<List<FicheSummaryResponse>>> GetAll([FromQuery] Guid? matiereId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var query = _db.Fiches.AsNoTracking().Where(f => f.UserId == userId);
        if (matiereId is { } m) query = query.Where(f => f.MatiereId == m);

        var fiches = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => FicheSummaryResponse.From(f))
            .ToListAsync(ct);

        return Ok(fiches);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FicheResponse>> GetOne(Guid id, CancellationToken ct)
    {
        var fiche = await Find(id, ct);
        return fiche is null ? NotFound() : Ok(FicheResponse.From(fiche));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var fiche = await Find(id, ct);
        if (fiche is null) return NotFound();

        _db.Fiches.Remove(fiche);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var fiche = await Find(id, ct);
        if (fiche is null) return NotFound();

        var bytes = FichePdfDocument.Generate(fiche);
        var fileName = $"{fiche.Titre.Trim().Replace(' ', '-')}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    private Task<Fiche?> Find(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        return _db.Fiches.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, ct);
    }
}
