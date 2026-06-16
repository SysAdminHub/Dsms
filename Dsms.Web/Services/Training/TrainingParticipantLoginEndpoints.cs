using Dsms.Web.Services.Training;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services;

/// <summary>
/// HTTP-Login für das Teilnehmerportal. Cookie wird hier gesetzt, nicht aus dem Blazor-Circuit.
/// </summary>
public static class TrainingParticipantLoginEndpoints
{
    public static void MapTrainingParticipantLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/schulung/teilnahme/login", LoginAsync);
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        TrainingParticipantPortalService portalService,
        IOptions<TrainingAccessOptions> options,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        await antiforgery.ValidateRequestAsync(context);

        var email = context.Request.Form["email"].ToString();
        var code = context.Request.Form["code"].ToString();

        var result = await portalService.LoginAsync(email, code, ct);
        var accessPath = options.Value.AccessPath;
        var contentPath = options.Value.PortalContentPath;

        if (result.Success)
            return Results.Redirect(contentPath);

        var message = Uri.EscapeDataString(
            result.Message ?? "Die eingegebenen Zugangsdaten sind ungültig oder abgelaufen.");
        return Results.Redirect($"{accessPath}?error={message}");
    }
}
