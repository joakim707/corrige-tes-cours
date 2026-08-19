using CorrigeTesCours.Api.Documents;
using CorrigeTesCours.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorrigeTesCours.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private const long MaxSizeBytes = 15 * 1024 * 1024; // 15 Mo
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".pptx", ".md", ".txt"
    };

    private readonly IDocumentTextExtractor _extractor;

    public DocumentsController(IDocumentTextExtractor extractor) => _extractor = extractor;

    [HttpPost("extract-text")]
    [RequestSizeLimit(MaxSizeBytes)]
    public async Task<ActionResult<ExtractedTextResponse>> ExtractText(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Fichier manquant", Status = 400 });

        if (file.Length > MaxSizeBytes)
            return BadRequest(new ProblemDetails { Title = "Fichier trop volumineux", Detail = "15 Mo maximum.", Status = 400 });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new ProblemDetails
            {
                Title = "Format non pris en charge",
                Detail = "Formats acceptés : .pdf, .docx, .pptx, .md",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        // L'extension seule est falsifiable : on vérifie aussi la signature binaire réelle du fichier.
        if (!await FileSignature.MatchesExtensionAsync(stream, extension))
            return BadRequest(new ProblemDetails
            {
                Title = "Fichier invalide",
                Detail = "Le contenu du fichier ne correspond pas à son extension.",
                Status = 400
            });
        stream.Position = 0;

        try
        {
            var text = _extractor.Extract(stream, file.FileName);
            return Ok(new ExtractedTextResponse(file.FileName, text, text.Length));
        }
        catch (UnsupportedDocumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Extraction impossible", Detail = ex.Message, Status = 400 });
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return BadRequest(new ProblemDetails { Title = "Fichier illisible ou corrompu", Detail = ex.Message, Status = 400 });
        }
    }
}
