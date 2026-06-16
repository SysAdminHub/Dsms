namespace Dsms.Web.Services.Training;

public interface ITrainingCertificatePdfService
{
    Task<TrainingCertificatePdfResult?> GenerateCertificateAsync(
        TrainingCompletionCertificateModel model,
        CancellationToken cancellationToken = default);
}

public sealed class TrainingCertificatePdfService(ILogger<TrainingCertificatePdfService> logger)
    : ITrainingCertificatePdfService
{
    public Task<TrainingCertificatePdfResult?> GenerateCertificateAsync(
        TrainingCompletionCertificateModel model,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fileName = TrainingCertificateFileNames.ForCertificate(
                model.TrainingTitle,
                model.ParticipantName,
                model.AssignmentId,
                model.CompletedAtUtc);

            var content = TrainingCertificatePdfComposer.Compose(model);
            return Task.FromResult<TrainingCertificatePdfResult?>(new TrainingCertificatePdfResult
            {
                Content = content,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "PDF-Teilnahmebescheinigung konnte nicht erzeugt werden (Assignment {AssignmentId})",
                model.AssignmentId);
            return Task.FromResult<TrainingCertificatePdfResult?>(null);
        }
    }
}
