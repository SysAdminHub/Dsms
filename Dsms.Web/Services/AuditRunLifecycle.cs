using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services;

/// <summary>
/// Zentrale Status- und Zeitstempel-Logik für Audit-Durchläufe (Start beim ersten Antwort-Speichern, Abschluss).
/// </summary>
public static class AuditRunLifecycle
{
    /// <summary>
    /// Setzt beim ersten Speichern einer Antwort einmalig Startdatum und Status „Laufend“ (aus Entwurf).
    /// </summary>
    /// <returns>true, wenn der Status auf InProgress gewechselt wurde (für Auditlog).</returns>
    public static bool ApplyStartOnAnswerSave(AuditRun run)
    {
        if (run.IsArchived || run.Status == AuditRunStatus.Completed)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var statusChanged = false;

        if (run.StartedAt is null)
        {
            run.StartedAt = now;
        }

        if (run.Status == AuditRunStatus.Draft)
        {
            run.Status = AuditRunStatus.InProgress;
            statusChanged = true;
        }

        run.UpdatedAt = now;
        return statusChanged;
    }

    /// <summary>
    /// Schließt einen Audit-Durchlauf ab. Startdatum wird bei Bedarf gesetzt; Abschlussdatum nur einmalig.
    /// </summary>
    /// <returns>true, wenn der Durchlauf abgeschlossen wurde.</returns>
    public static bool ApplyComplete(AuditRun run)
    {
        if (run.IsArchived || run.Status == AuditRunStatus.Completed)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        if (run.StartedAt is null)
        {
            run.StartedAt = now;
        }

        run.Status = AuditRunStatus.Completed;

        if (run.CompletedAt is null)
        {
            run.CompletedAt = now;
        }

        run.UpdatedAt = now;
        return true;
    }
}
