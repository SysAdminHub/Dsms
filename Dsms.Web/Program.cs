using Dsms.Web.Components;
using Dsms.Web.Components.Account;
using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Blazor Server (interaktive Komponenten) ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// --- Authentifizierung / Autorisierung (Identity + Blazor Auth-State) ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Dsms.Session";
});

builder.Services.AddScoped<TenantContextAccessor>();
builder.Services.AddScoped<ArchiveViewContextAccessor>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IArchivingService, ArchivingService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProcessingActivityRelationsService>();
builder.Services.AddScoped<DocumentStorageService>();
builder.Services.AddScoped<DocumentLinksService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// --- Datenbank (MySQL via Pomelo) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
void ConfigureDbContext(DbContextOptionsBuilder options) =>
    options.UseMySql(connectionString, serverVersion);

// Scoped DbContext für Identity. Factory ebenfalls Scoped, weil ApplicationDbContext
// TenantContextAccessor (scoped) injiziert – Singleton-Factory würde DI-Validierung scheitern.
builder.Services.AddDbContext<ApplicationDbContext>(ConfigureDbContext);
builder.Services.AddDbContextFactory<ApplicationDbContext>(ConfigureDbContext, ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // Demo/Intern: sofortiger Login nach Seed
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Kein echter Mailversand in Version 1.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantInitialization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Mandantenwechsel per Form-POST (kein paralleler DbContext-Zugriff in Blazor-Komponenten).
app.MapPost("/tenant/switch", async (
    HttpContext context,
    ITenantService tenantService,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (!int.TryParse(context.Request.Form["tenantId"], out var tenantId))
    {
        return Results.Redirect("/select-tenant");
    }

    var result = await tenantService.SwitchTenantAsync(tenantId);
    if (!result.Succeeded)
    {
        return Results.Redirect("/select-tenant");
    }

    var returnUrl = context.Request.Headers.Referer.FirstOrDefault();
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
})
.RequireAuthorization();

// Minimal-API-Endpunkte für Identity-Formulare (Logout, externe Logins, …).
app.MapAdditionalIdentityEndpoints();
app.MapDocumentFileEndpoints();

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();
