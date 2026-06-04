namespace Dsms.Web.Domain.Enums;



/// <summary>Rolle des Dienstleisters in einer konkreten Verarbeitungstätigkeit (Verknüpfungstabelle).</summary>

public enum ProcessingRole

{

    DataProcessor = 0,

    SubProcessor = 1,

    Recipient = 2,

    MaintenanceProvider = 3,

    SoftwareVendor = 4,

    HostingProvider = 5,

    Other = 6

}

