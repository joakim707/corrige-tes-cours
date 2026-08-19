namespace CorrigeTesCours.Api.Documents;

/// <summary>
/// Vérifie les premiers octets d'un fichier plutôt que de se fier à son extension,
/// qui peut être renommée sans changer le contenu réel.
/// </summary>
public static class FileSignature
{
    private static readonly byte[] Pdf = "%PDF"u8.ToArray();
    private static readonly byte[] Zip = { 0x50, 0x4B, 0x03, 0x04 }; // .docx / .pptx (formats OOXML = zip)

    public static async Task<bool> MatchesExtensionAsync(Stream stream, string extension)
    {
        // Le texte brut (.md/.txt) n'a pas de signature binaire fiable à vérifier.
        if (extension is ".md" or ".txt") return true;

        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4));
        stream.Position = 0;
        if (read < 4) return false;

        return extension switch
        {
            ".pdf" => header.AsSpan(0, 4).SequenceEqual(Pdf),
            ".docx" or ".pptx" => header.AsSpan(0, 4).SequenceEqual(Zip),
            _ => false
        };
    }
}
