namespace Dsms.Web.Services.Training;

/// <summary>Konfiguration für Schulungs-Zugangscodes und Sperrlogik.</summary>
public class TrainingAccessOptions
{
    public const string SectionName = "TrainingAccess";

    public int DefaultValidityDays { get; set; } = 14;
    public int MaxValidityDays { get; set; } = 90;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public string AccessPath { get; set; } = "/schulung/teilnahme";
}
