using Dsms.Web.Domain.Enums;



namespace Dsms.Web.Domain.Entities;



/// <summary>

/// Externer Dienstleister oder Auftragsverarbeiter im mandantenbezogenen Verzeichnis.

/// Dokumentiert AVV, TOM-Prüfung, Drittlandbezug und Verknüpfungen zu Verarbeitungstätigkeiten und TOMs.

/// </summary>

public class ServiceProvider : ArchivableEntityBase, ITenantEntity

{

    public int TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;



    /// <summary>Name des Dienstleisters.</summary>

    public string Name { get; set; } = string.Empty;



    public string? Description { get; set; }



    public ServiceProviderType ProviderType { get; set; } = ServiceProviderType.Other;



    /// <summary>Dienstleistung / Zweck der Beauftragung.</summary>

    public string? ServicePurpose { get; set; }



    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Address { get; set; }

    public string? Country { get; set; }



    /// <summary>Handelt der Dienstleister als Auftragsverarbeiter (Art. 28 DSGVO)?</summary>

    public bool IsDataProcessor { get; set; }



    /// <summary>Auftragsverarbeitungsvertrag (AVV) liegt vor.</summary>

    public bool DataProcessingAgreementExists { get; set; }



    public DateOnly? DataProcessingAgreementDate { get; set; }



    /// <summary>Datum der letzten AVV-Prüfung.</summary>

    public DateOnly? DataProcessingAgreementReviewedAt { get; set; }



    /// <summary>Ergebnis oder Kurzfassung der AVV-Prüfung.</summary>

    public string? DataProcessingAgreementReviewResult { get; set; }



    /// <summary>TOMs beim Dienstleister geprüft oder vereinbart.</summary>

    public bool TomsReviewed { get; set; }



    public DateOnly? TomsReviewedAt { get; set; }



    public string? TomsReviewResult { get; set; }



    public bool SubProcessorsAllowed { get; set; }



    public string? SubProcessorsDescription { get; set; }



    public bool ThirdCountryInvolvement { get; set; }



    public string? ThirdCountry { get; set; }



    public ThirdCountryTransferLegalBasis ThirdCountryLegalBasis { get; set; } =

        ThirdCountryTransferLegalBasis.NoThirdCountryTransfer;



    public string? ThirdCountryTransferGuarantees { get; set; }



    public ServiceProviderRiskAssessment RiskAssessment { get; set; } = ServiceProviderRiskAssessment.NotAssessed;



    public ServiceProviderStatus Status { get; set; } = ServiceProviderStatus.InReview;



    public string? ResponsiblePerson { get; set; }



    public string? Notes { get; set; }



    public ICollection<ProcessingActivityServiceProvider> ProcessingActivityLinks { get; set; } = [];

    public ICollection<ServiceProviderTom> TomLinks { get; set; } = [];
}

