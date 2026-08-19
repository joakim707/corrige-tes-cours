using CorrigeTesCours.Api.Auth;
using CorrigeTesCours.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refresh_token";

    private readonly IAuthService _auth;
    private readonly JwtOptions _options;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, IOptions<JwtOptions> options, IWebHostEnvironment env)
    {
        _auth = auth;
        _options = options.Value;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var (auth, refresh) = await _auth.RegisterAsync(request, ct);
            SetRefreshCookie(refresh);
            return Ok(auth);
        }
        catch (AuthException ex)
        {
            return Conflict(new ProblemDetails { Title = "Inscription impossible", Detail = ex.Message, Status = 409 });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        try
        {
            var (auth, refresh) = await _auth.LoginAsync(request, ct);
            SetRefreshCookie(refresh);
            return Ok(auth);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails { Title = "Connexion refusée", Detail = ex.Message, Status = 401 });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var raw) || string.IsNullOrWhiteSpace(raw))
            return Unauthorized(new ProblemDetails { Title = "Aucun refresh token", Status = 401 });

        try
        {
            var (auth, refresh) = await _auth.RefreshAsync(raw, ct);
            SetRefreshCookie(refresh);
            return Ok(auth);
        }
        catch (AuthException ex)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized(new ProblemDetails { Title = "Session expirée", Detail = ex.Message, Status = 401 });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var raw) && !string.IsNullOrWhiteSpace(raw))
            await _auth.RevokeAsync(raw, ct);

        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    private void SetRefreshCookie(string rawToken)
    {
        Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays)
        });
    }
}
