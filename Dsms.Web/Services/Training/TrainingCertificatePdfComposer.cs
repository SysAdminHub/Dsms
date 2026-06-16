using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dsms.Web.Services.Training;

internal static class TrainingCertificatePdfComposer
{
    private const string ProductName = "Datenschutz-Cloud";

    public static byte[] Compose(TrainingCompletionCertificateModel model) =>
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(48);
                page.MarginHorizontal(56);
                page.DefaultTextStyle(x => x.FontSize(11).LineHeight(1.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().AlignCenter().Text("Teilnahmebescheinigung")
                        .Bold().FontSize(22).FontColor(Colors.Grey.Darken4);
                    header.Item().PaddingTop(8).AlignCenter().Text("Nachweis über die erfolgreiche Teilnahme an einer Datenschutzschulung")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    header.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(24).Column(column =>
                {
                    column.Spacing(8);
                    AddRow(column, "Organisation / Mandant", model.OrganizationName);
                    AddRow(column, "Teilnehmer", model.ParticipantName);

                    if (!string.IsNullOrWhiteSpace(model.ParticipantEmail))
                        AddRow(column, "E-Mail", model.ParticipantEmail);

                    AddRow(column, "Schulung", model.TrainingTitle);
                    AddRow(column, "Abschlussdatum", TrainingCertificateFormatting.FormatDateTime(model.CompletedAtUtc));
                    AddRow(column, "Status", ResolveStatusLabel(model));

                    if (model.HasQuiz)
                    {
                        if (model.QuizScorePercent is int score)
                            AddRow(column, "Quiz-Ergebnis", $"{score} %");

                        if (model.QuizQuestionCount is int questionCount)
                            AddRow(column, "Anzahl Fragen", questionCount.ToString());

                        if (model.QuizCorrectQuestionCount is int correctCount && model.QuizQuestionCount is int total)
                            AddRow(column, "Korrekt beantwortet", $"{correctCount} von {total}");
                    }

                    AddRow(column, "Nachweisnummer", model.AssignmentId.ToString());
                    AddRow(column, "Erstellt am", TrainingCertificateFormatting.FormatDateTime(model.GeneratedAtUtc));

                    column.Item().PaddingTop(20).Text(
                        "Hiermit wird bestätigt, dass die oben genannte Person die Schulung erfolgreich abgeschlossen hat.");

                    if (model.HasQuiz)
                    {
                        column.Item().PaddingTop(8).Text(
                            "Falls ein Quiz Bestandteil der Schulung war, wird das erreichte Ergebnis als Nachweis dokumentiert.");
                    }
                });

                page.Footer().AlignCenter().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(8).Text($"Erstellt durch {ProductName}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

    private static string ResolveStatusLabel(TrainingCompletionCertificateModel model)
    {
        if (model.HasQuiz && model.QuizPassed == true)
            return "Bestanden";

        return "Abgeschlossen";
    }

    private static void AddRow(ColumnDescriptor column, string label, string value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(160).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
    }
}
