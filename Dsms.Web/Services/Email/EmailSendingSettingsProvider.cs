using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Email;

public sealed class EmailSendingSettingsProvider(IDbContextFactory<ApplicationDbContext> dbFactory)
    : IEmailSendingSettingsProvider
{
    public async Task<EmailSettings?> GetSettingsForSendingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.EmailSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
