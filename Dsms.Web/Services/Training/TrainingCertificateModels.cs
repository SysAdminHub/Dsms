using System.Globalization;

namespace Dsms.Web.Services.Training;

public sealed class TrainingCompletionCertificateModel
{
    public required string OrganizationName { get; init; }
    public required string ParticipantName { get; init; }
    public string? ParticipantEmail { get; init; }
    public required string TrainingTitle { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
    public required int AssignmentId { get; init; }
    public bool HasQuiz { get; init; }
    public bool? QuizPassed { get; init; }
    public int? QuizScorePercent { get; init; }
    public int? QuizQuestionCount { get; init; }
    public int? QuizCorrectQuestionCount { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}

public sealed class TrainingCertificatePdfResult
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/pdf";
}

public sealed record TrainingParticipantCertificateInfo(
    int? DocumentId,
    string? DownloadPath,
    bool GenerationFailed,
    string? FileName);

public static class TrainingCertificateFileNames
{
    public static string ForCertificate(
        string trainingTitle,
        string participantName,
        int assignmentId,
        DateTime completedAtUtc)
    {
        var timestamp = completedAtUtc.ToLocalTime().ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var trainingPart = Sanitize(trainingTitle, 40);
        var participantPart = Sanitize(participantName, 40);
        return Sanitize($"Teilnahmebescheinigung_{trainingPart}_{participantPart}_{timestamp}.pdf");
    }

    public static string BuildDocumentTitle(string trainingTitle, string participantName) =>
        $"Teilnahmebescheinigung - {trainingTitle.Trim()} - {participantName.Trim()}";

    private static string Sanitize(string value, int maxLength = 120)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            trimmed = trimmed[..maxLength].TrimEnd();

        var chars = trimmed
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' ? ch : '_')
            .ToArray();

        var result = new string(chars).Trim('_');
        return string.IsNullOrEmpty(result) ? "unbenannt" : result;
    }
}

public static class TrainingCertificateFormatting
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatDateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm 'Uhr'", GermanCulture);
}
