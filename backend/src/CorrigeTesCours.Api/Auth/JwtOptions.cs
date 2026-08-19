namespace CorrigeTesCours.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "corrige-tes-cours";
    public string Audience { get; set; } = "corrige-tes-cours-web";
    /// <summary>Clé de signature HS256 — au moins 32 caractères, jamais commitée.</summary>
    public string Secret { get; set; } = null!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
