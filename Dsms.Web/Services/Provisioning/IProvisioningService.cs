namespace Dsms.Web.Services.Provisioning;

public interface IProvisioningService
{
    Task<ProvisionCustomerResultDto> ProvisionCustomerAsync(ProvisionCustomerRequestDto dto);
}
