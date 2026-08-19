using CorrigeTesCours.Api.Dtos;
using CorrigeTesCours.Domain.Entities;
using CorrigeTesCours.Infrastructure.Persistence;
using CorrigeTesCours.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CorrigeTesCours.Api.Auth;

/// <summary>Erreur métier d'authentification, traduite en 4xx par le contrôleur.</summary>
public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}

public interface IAuthService
{
    Task<(AuthResponse Auth, string RefreshToken)> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<(AuthResponse Auth, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<(AuthResponse Auth, string RefreshToken)> RefreshAsync(string rawRefreshToken, CancellationToken ct);
    Task RevokeAsync(string rawRefreshToken, CancellationToken ct);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _options;

    public AuthService(AppDbContext db, IPasswordHasher hasher, ITokenService tokens, IOptions<JwtOptions> options)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _options = options.Value;
    }

    public async Task<(AuthResponse, string)> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new AuthException("Un compte existe déjà avec cet email.");

        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Pseudo = request.Pseudo.Trim(),
            Level = request.Level
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<(AuthResponse, string)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Même message dans les deux cas : ne pas révéler l'existence du compte.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Email ou mot de passe incorrect.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<(AuthResponse, string)> RefreshAsync(string rawRefreshToken, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(rawRefreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive)
            throw new AuthException("Refresh token invalide ou expiré.");

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task RevokeAsync(string rawRefreshToken, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(rawRefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || stored.RevokedAt is not null) return;

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(AuthResponse, string)> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (raw, hash) = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays)
        });
        await _db.SaveChangesAsync(ct);

        var access = _tokens.CreateAccessToken(user);
        var response = new AuthResponse(access, _options.AccessTokenMinutes * 60, UserResponse.From(user));
        return (response, raw);
    }
}
