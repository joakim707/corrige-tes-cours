using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Domain.Entities;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/matieres")]
[Authorize]
public class MatieresController : ControllerBase
{
    private readonly AppDbContext _db;

    public MatieresController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MatiereResponse>>> GetAll(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var matieres = await _db.Matieres
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Nom)
            .Select(m => MatiereResponse.From(m))
            .ToListAsync(ct);

        return Ok(matieres);
    }

    [HttpPost]
    public async Task<ActionResult<MatiereResponse>> Create(CreateMatiereRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var nom = request.Nom.Trim();

        var exists = await _db.Matieres.AnyAsync(m => m.UserId == userId && m.Nom == nom, ct);
        if (exists)
            return Conflict(new ProblemDetails { Title = "Matière déjà existante", Detail = $"Une matière nommée « {nom} » existe déjà.", Status = 409 });

        var matiere = new Matiere
        {
            Nom = nom,
            Couleur = request.Couleur,
            Niveau = request.Niveau,
            UserId = userId
        };

        _db.Matieres.Add(matiere);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { }, MatiereResponse.From(matiere));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var matiere = await _db.Matieres.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);
        if (matiere is null) return NotFound();

        _db.Matieres.Remove(matiere);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
