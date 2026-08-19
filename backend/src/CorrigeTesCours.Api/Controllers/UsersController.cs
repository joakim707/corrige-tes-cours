using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? NotFound() : Ok(UserResponse.From(user));
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Identifiant de l'utilisateur porté par le claim `sub` du JWT.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Le JWT ne contient pas d'identifiant utilisateur valide.");
    }
}
