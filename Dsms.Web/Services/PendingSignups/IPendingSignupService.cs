namespace Dsms.Web.Services.PendingSignups;

public interface IPendingSignupService
{
    Task<IReadOnlyList<PendingSignupListDto>> GetAllAsync(
        string? search = null,
        string? statusFilter = null,
        Guid? planIdFilter = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null);

    Task<PendingSignupDetailsDto?> GetByIdAsync(Guid id);

    Task<Guid> CreateAsync(CreatePendingSignupDto dto);

    /// <summary>Öffentlicher Paid-Signup ohne Superuser-Prüfung – nur aktive bezahlte Pläne.</summary>
    Task<Guid> CreatePublicAsync(CreatePendingSignupDto dto);

    Task<bool> UpdateStatusAsync(Guid id, string status, string? note = null);

    Task<bool> MarkAsPaidAsync(Guid id, string? externalPaymentId = null);

    Task<bool> MarkAsProvisionedAsync(
        Guid id,
        Guid licenseId,
        string? licenseNumber,
        int tenantId,
        string? tenantName,
        string adminUserId);

    Task<bool> MarkAsFailedAsync(Guid id, string errorMessage);

    Task<bool> MarkAsCancelledAsync(Guid id, string? note = null);

    Task<bool> MarkAsExpiredAsync(Guid id, string? note = null);

    Task<bool> UpdateInternalNoteAsync(Guid id, string? internalNote);

    Task<IReadOnlyList<PendingSignupListDto>> GetPendingPaymentAsync();

    Task<IReadOnlyList<PendingSignupListDto>> GetExpiredAsync();

    Task<PendingSignupDetailsDto?> GetByExternalPaymentIdAsync(string externalPaymentId);
}
