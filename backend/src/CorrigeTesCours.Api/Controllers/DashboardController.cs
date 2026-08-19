using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> Stats(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var matieresCount = await _db.Matieres.CountAsync(m => m.UserId == userId, ct);
        var fichesCount = await _db.Fiches.CountAsync(f => f.UserId == userId, ct);
        var quizCount = await _db.Quizzes.CountAsync(q => q.UserId == userId, ct);
        var scores = await _db.QuizResults
            .Where(r => r.UserId == userId)
            .Select(r => r.Score)
            .ToListAsync(ct);

        double? scoreMoyen = scores.Count > 0 ? scores.Average() : null;

        return Ok(new DashboardStatsResponse(matieresCount, fichesCount, quizCount, scoreMoyen));
    }
}
