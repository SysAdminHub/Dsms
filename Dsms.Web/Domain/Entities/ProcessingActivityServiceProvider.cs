using Dsms.Web.Domain.Enums;



namespace Dsms.Web.Domain.Entities;



/// <summary>

/// Verknüpfung zwischen Dienstleister und Verarbeitungstätigkeit (Many-to-Many).

/// TenantId sichert mandantenübergreifende Verknüpfungen ab.

/// </summary>

public class ProcessingActivityServiceProvider

{

    public int Id { get; set; }



    public int TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;



    public int ProcessingActivityId { get; set; }

    public ProcessingActivity ProcessingActivity { get; set; } = null!;



    public int ServiceProviderId { get; set; }

    public ServiceProvider ServiceProvider { get; set; } = null!;



    public ProcessingRole RoleInProcessing { get; set; } = ProcessingRole.DataProcessor;



    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}

