using System.Security.Cryptography;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

/// <summary>Erzeugung, Hashing und Verifikation von 6-stelligen Schulungs-Zugangscodes.</summary>
public class TrainingAccessCodeService(IOptions<TrainingAccessOptions> options)
{
    private readonly PasswordHasher<TrainingAssignment> _hasher = new();
    private readonly TrainingAccessOptions _options = options.Value;

    public int DefaultValidityDays => _options.DefaultValidityDays;
    public int MaxValidityDays => _options.MaxValidityDays;
    public int MaxFailedAccessAttempts => _options.MaxFailedAccessAttempts;
    public int LockoutMinutes => _options.LockoutMinutes;

    public string GenerateCode()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000;
        return value.ToString("D6");
    }

    public string HashCode(TrainingAssignment assignment, string code) =>
        _hasher.HashPassword(assignment, code);

    public bool VerifyCode(TrainingAssignment assignment, string code, string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        return _hasher.VerifyHashedPassword(assignment, hash, code) == PasswordVerificationResult.Success;
    }

    public DateTime GetExpiryUtc(DateTime generatedAtUtc, int validityDays) =>
        generatedAtUtc.AddDays(validityDays);

    public int ResolveValidityDays(TrainingEntity training)
    {
        if (training.AccessCodeValidityDays >= 1 && training.AccessCodeValidityDays <= _options.MaxValidityDays)
            return training.AccessCodeValidityDays;

        return _options.DefaultValidityDays > 0 ? _options.DefaultValidityDays : 14;
    }

    public bool IsCodeExpired(TrainingAssignment assignment, DateTime utcNow)
    {
        if (assignment.AccessCodeExpiresAtUtc is null)
            return false;

        return assignment.AccessCodeExpiresAtUtc <= utcNow;
    }

    public bool IsLocked(TrainingAssignment assignment, DateTime utcNow) =>
        assignment.LockedUntilUtc is not null && assignment.LockedUntilUtc > utcNow;

    /// <summary>
    /// Validiert Zugangscode für späteres Teilnehmerportal (Prompt 5).
    /// TODO: Vollständige Portal-Integration in Prompt 5.
    /// </summary>
    public TrainingAccessCodeValidationResult ValidateAccessCode(
        TrainingAssignment assignment,
        string code,
        DateTime utcNow)
    {
        if (assignment.Status == TrainingAssignmentStatus.Cancelled)
            return TrainingAccessCodeValidationResult.Cancelled;

        if (IsLocked(assignment, utcNow))
            return TrainingAccessCodeValidationResult.Locked;

        if (string.IsNullOrWhiteSpace(assignment.AccessCodeHash))
            return TrainingAccessCodeValidationResult.NotFound;

        if (IsCodeExpired(assignment, utcNow))
            return TrainingAccessCodeValidationResult.Expired;

        return VerifyCode(assignment, code, assignment.AccessCodeHash)
            ? TrainingAccessCodeValidationResult.Valid
            : TrainingAccessCodeValidationResult.InvalidCode;
    }

    public void ApplyFailedAccessAttempt(TrainingAssignment assignment, DateTime utcNow)
    {
        assignment.LastAccessAttemptAtUtc = utcNow;
        assignment.FailedAccessAttempts++;

        if (assignment.FailedAccessAttempts >= _options.MaxFailedAccessAttempts)
        {
            assignment.LockedUntilUtc = utcNow.AddMinutes(_options.LockoutMinutes);
            assignment.Status = TrainingAssignmentStatus.Locked;
        }
    }

    public void ResetFailedAccessAttempts(TrainingAssignment assignment)
    {
        assignment.FailedAccessAttempts = 0;
        assignment.LastAccessAttemptAtUtc = null;
        assignment.LockedUntilUtc = null;

        if (assignment.Status == TrainingAssignmentStatus.Locked)
            assignment.Status = TrainingAssignmentStatus.Invited;
    }
}
