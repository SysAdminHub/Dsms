namespace Dsms.Web.Services;

/// <summary>Scoped-Cache für aktiven Supportmodus (Grant-ID aus Session).</summary>
public class SupportContextAccessor
{
    public int? ActiveSupportAccessGrantId { get; set; }

    public bool IsSupportMode => ActiveSupportAccessGrantId.HasValue;
}
