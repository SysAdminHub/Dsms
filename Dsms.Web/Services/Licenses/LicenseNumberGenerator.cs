using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Licenses;

public static class LicenseNumberGenerator
{
    public static async Task<string> GenerateAsync(ApplicationDbContext db)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"LIC-{year}-";

        var existingNumbers = await db.Licenses
            .Where(l => l.LicenseNumber.StartsWith(prefix))
            .Select(l => l.LicenseNumber)
            .ToListAsync();

        var maxSequence = 0;
        foreach (var number in existingNumbers)
        {
            if (number.Length > prefix.Length
                && int.TryParse(number[prefix.Length..], out var sequence))
            {
                maxSequence = Math.Max(maxSequence, sequence);
            }
        }

        return $"{prefix}{(maxSequence + 1):D6}";
    }
}
