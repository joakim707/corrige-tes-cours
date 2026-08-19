using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace CorrigeTesCours.Api.Documents;

/// <summary>Extension non reconnue ou fichier corrompu — traduit en 400 par le contrôleur.</summary>
public class UnsupportedDocumentException : Exception
{
    public UnsupportedDocumentException(string message) : base(message) { }
}

public interface IDocumentTextExtractor
{
    /// <summary>Extrait le texte brut d'un fichier .pdf, .docx, .pptx ou .md à partir de son extension.</summary>
    string Extract(Stream content, string fileName);
}

public class DocumentTextExtractor : IDocumentTextExtractor
{
    public string Extract(Stream content, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var text = extension switch
        {
            ".pdf" => ExtractPdf(content),
            ".docx" => ExtractDocx(content),
            ".pptx" => ExtractPptx(content),
            ".md" or ".txt" => ExtractPlainText(content),
            _ => throw new UnsupportedDocumentException($"Format « {extension} » non pris en charge (formats acceptés : .pdf, .docx, .pptx, .md).")
        };

        if (string.IsNullOrWhiteSpace(text))
            throw new UnsupportedDocumentException("Aucun texte n'a pu être extrait de ce fichier.");

        return text.Trim();
    }

    private static string ExtractPdf(Stream content)
    {
        using var document = PdfDocument.Open(content);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocx(Stream content)
    {
        using var document = WordprocessingDocument.Open(content, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null) return "";

        var sb = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
            sb.AppendLine(paragraph.InnerText);
        return sb.ToString();
    }

    private static string ExtractPptx(Stream content)
    {
        using var document = PresentationDocument.Open(content, false);
        var presentationPart = document.PresentationPart;
        if (presentationPart is null) return "";

        var sb = new StringBuilder();
        foreach (var slidePart in presentationPart.SlideParts)
        {
            var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var t in texts)
                sb.AppendLine(t.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExtractPlainText(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
