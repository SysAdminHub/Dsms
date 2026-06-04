namespace Dsms.Web.Domain.Entities;



/// <summary>

/// Verknüpfung zwischen Dienstleister und TOM (Many-to-Many) – dokumentiert geprüfte/vereinbarte TOMs.

/// </summary>

public class ServiceProviderTom

{

    public int Id { get; set; }



    public int TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;



    public int ServiceProviderId { get; set; }

    public ServiceProvider ServiceProvider { get; set; } = null!;



    public int TomId { get; set; }

    public Tom Tom { get; set; } = null!;



    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}

