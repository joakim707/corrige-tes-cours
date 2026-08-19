using CorrigeTesCours.Infrastructure.Security;
using Xunit;

namespace CorrigeTesCours.Api.Tests;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProduitDesHashsDifferentsPourLeMemeMotDePasse()
    {
        // Le sel aléatoire de BCrypt garantit deux hashs différents pour la même entrée.
        var hash1 = _hasher.Hash("motdepasse123");
        var hash2 = _hasher.Hash("motdepasse123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_AccepteLeBonMotDePasse()
    {
        var hash = _hasher.Hash("motdepasse123");

        Assert.True(_hasher.Verify("motdepasse123", hash));
    }

    [Fact]
    public void Verify_RejetteUnMauvaisMotDePasse()
    {
        var hash = _hasher.Hash("motdepasse123");

        Assert.False(_hasher.Verify("autrechose", hash));
    }

    [Fact]
    public void Verify_NeCrashePasSurUnHashInvalide()
    {
        // Défense en profondeur : un hash corrompu ne doit jamais faire planter l'auth.
        Assert.False(_hasher.Verify("motdepasse123", "pas-un-hash-bcrypt-valide"));
    }
}
