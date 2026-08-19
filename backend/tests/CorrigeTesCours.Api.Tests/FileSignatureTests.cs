using System.Text;
using CorrigeTesCours.Api.Documents;
using Xunit;

namespace CorrigeTesCours.Api.Tests;

public class FileSignatureTests
{
    [Fact]
    public async Task MatchesExtensionAsync_Pdf_AccepteSignatureValide()
    {
        using var stream = BuildStream("%PDF-1.7 reste du fichier...");

        Assert.True(await FileSignature.MatchesExtensionAsync(stream, ".pdf"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_Pdf_RejetteFichierRenommeSansVraieSignature()
    {
        // Un .txt renommé en .pdf ne doit pas passer, même si l'extension trompe le filtre.
        using var stream = BuildStream("Ceci est juste du texte brut.");

        Assert.False(await FileSignature.MatchesExtensionAsync(stream, ".pdf"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_Docx_AccepteSignatureZip()
    {
        using var stream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 });

        Assert.True(await FileSignature.MatchesExtensionAsync(stream, ".docx"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_MarkdownEtTexte_ToujoursAcceptes()
    {
        using var stream = BuildStream("# Titre\nContenu markdown");

        Assert.True(await FileSignature.MatchesExtensionAsync(stream, ".md"));
        stream.Position = 0;
        Assert.True(await FileSignature.MatchesExtensionAsync(stream, ".txt"));
    }

    [Fact]
    public async Task MatchesExtensionAsync_FichierTropCourt_EstRejete()
    {
        using var stream = new MemoryStream(new byte[] { 0x50, 0x4B });

        Assert.False(await FileSignature.MatchesExtensionAsync(stream, ".docx"));
    }

    private static MemoryStream BuildStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
