using CorrigeTesCours.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CorrigeTesCours.Api.Pdf;

public static class FichePdfDocument
{
    public static byte[] Generate(Fiche fiche)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text(fiche.Titre).FontSize(20).Bold();

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Résumé").FontSize(14).Bold();
                    col.Item().Text(fiche.Resume);

                    if (fiche.PointsCles.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Points clés").FontSize(14).Bold();
                        foreach (var point in fiche.PointsCles)
                            col.Item().Text($"•  {point}");
                    }

                    if (fiche.Definitions.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Définitions").FontSize(14).Bold();
                        foreach (var (terme, def) in fiche.Definitions)
                            col.Item().Text(t =>
                            {
                                t.Span($"{terme} — ").Bold();
                                t.Span(def);
                            });
                    }

                    if (fiche.Formules.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Formules").FontSize(14).Bold();
                        foreach (var formule in fiche.Formules)
                            col.Item().Text(formule);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Généré par Corrige tes cours — ").FontSize(8);
                    t.Span(fiche.CreatedAt.ToString("dd/MM/yyyy")).FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }
}
