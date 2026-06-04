namespace Dsms.Web.Domain.Enums;



/// <summary>Rechtsgrundlage oder Bewertungsstatus bei Drittlandbezug.</summary>

public enum ThirdCountryTransferLegalBasis

{

    NoThirdCountryTransfer = 0,

    AdequacyDecision = 1,

    EuStandardContractualClauses = 2,

    ExceptionRule = 3,

    ToBeReviewed = 4,

    Other = 5

}

