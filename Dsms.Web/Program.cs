using Dsms.Web.Components;
using Dsms.Web.Components.Account;
using Dsms.Web.Configuration;
using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Services;
using Dsms.Web.Services.CommunityTemplates;
using Dsms.Web.Services.TenantDeletion;
using Dsms.Web.Services.TenantExport;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.PasswordReset;
using Dsms.Web.Services.Reminders;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.PendingSignups;
using Dsms.Web.Services.Provisioning;
using Dsms.Web.Services.Signup;
using Dsms.Web.Services.SubscriptionPlans;
using Dsms.Web.Services.DiscountCodes;
using Dsms.Web.Services.UpgradeRequests;
using Dsms.Web.Services.Feedback;
using Dsms.Web.Services.Tenants;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.Onboarding;
using Dsms.Web.Services.PageHelp;
using Dsms.Web.Services.Legal;
using Dsms.Web.Services.Privacy;
using Dsms.Web.Services.Training;
using Dsms.Web.Services.Support;
using Microsoft.AspNetCore.Components.Authorization;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<AppBrandingOptions>(
    builder.Configuration.GetSection(AppBrandingOptions.SectionName));
builder.Services.Configure<AppUrlOptions>(
    builder.Configuration.GetSection(AppUrlOptions.SectionName));

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
builder.Services.AddScoped<SupportContextAccessor>();
builder.Services.AddScoped<ArchiveViewContextAccessor>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();
builder.Services.AddScoped<ISupportContextService, SupportContextService>();
builder.Services.AddScoped<ISupportAccessNotificationService, SupportAccessNotificationService>();
builder.Services.AddScoped<ISupportAccessService, SupportAccessService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IArchivingService, ArchivingService>();
builder.Services.AddScoped<IAuditTemplateService, AuditTemplateService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
builder.Services.AddScoped<IPageHelpContentService, PageHelpContentService>();
builder.Services.AddScoped<ProcessingActivityRelationsService>();
builder.Services.AddScoped<PrivacyIncidentRelationsService>();
builder.Services.AddScoped<DataSubjectRequestRelationsService>();
builder.Services.AddScoped<DataSubjectRequestService>();
builder.Services.AddScoped<DocumentStorageService>();
builder.Services.AddScoped<DocumentLinksService>();
builder.Services.AddScoped<DocumentCategoryService>();
builder.Services.AddScoped<TomCategoryService>();
builder.Services.AddScoped<DataProtectionRoleService>();
builder.Services.AddScoped<ITenantExportService, TenantExportService>();
builder.Services.AddScoped<ITenantDeletionService, TenantDeletionService>();
builder.Services.AddScoped<ITenantDeletionNotificationService, TenantDeletionNotificationService>();
builder.Services.AddScoped<ITenantDataErasureService, TenantDataErasureService>();

builder.Services.Configure<Dsms.Web.Configuration.DataProtectionOptions>(
    builder.Configuration.GetSection(Dsms.Web.Configuration.DataProtectionOptions.SectionName));

var dataProtectionOptions = builder.Configuration
    .GetSection(Dsms.Web.Configuration.DataProtectionOptions.SectionName)
    .Get<Dsms.Web.Configuration.DataProtectionOptions>()
    ?? new Dsms.Web.Configuration.DataProtectionOptions();

var dataProtectionKeysPath = Path.IsPathRooted(dataProtectionOptions.KeysPath)
    ? dataProtectionOptions.KeysPath
    : Path.Combine(builder.Environment.ContentRootPath, dataProtectionOptions.KeysPath);
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .SetApplicationName(dataProtectionOptions.ApplicationName)
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddScoped<IEmailSecretProtector, EmailSecretProtector>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSettingsService, EmailSettingsService>();
builder.Services.AddScoped<IEmailSendingSettingsProvider, EmailSendingSettingsProvider>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILicenseFeatureService, LicenseFeatureService>();
builder.Services.AddScoped<IPlanToLicenseService, PlanToLicenseService>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddScoped<IPublicSignupService, PublicSignupService>();
builder.Services.AddScoped<ISignupNotificationService, SignupNotificationService>();
builder.Services.AddScoped<ICommunityTemplateNotificationService, CommunityTemplateNotificationService>();
builder.Services.AddScoped<ISignupLegalEmailService, SignupLegalEmailService>();
builder.Services.AddScoped<IUpgradeRequestService, UpgradeRequestService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<FeedbackModalState>();
builder.Services.AddScoped<IPaidSignupService, PaidSignupService>();
builder.Services.AddScoped<IPendingSignupService, PendingSignupService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<IDiscountCodeValidationService, DiscountCodeValidationService>();
builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();
builder.Services.AddScoped<ITenantComplianceInfoService, TenantComplianceInfoService>();
builder.Services.AddScoped<ILegalDocumentService, LegalDocumentService>();
builder.Services.AddScoped<ILegalPdfService, LegalPdfService>();
builder.Services.AddScoped<ILegalAcceptanceService, LegalAcceptanceService>();
builder.Services.AddScoped<IIpAnonymizationService, IpAnonymizationService>();
builder.Services.AddScoped<ILegalPlaceholderService, LegalPlaceholderService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<ILicenseCreateGuard, LicenseCreateGuard>();
builder.Services.AddScoped<IComplianceAuditLogService, ComplianceAuditLogService>();
builder.Services.AddScoped<TrainingTemplateAccessService>();
builder.Services.AddScoped<TrainingAssetStorageService>();
builder.Services.AddScoped<TrainingTemplateAssetService>();
builder.Services.AddScoped<TrainingQuestionService>();
builder.Services.AddScoped<TrainingTemplateService>();
builder.Services.AddScoped<TrainingService>();
builder.Services.Configure<TrainingAccessOptions>(builder.Configuration.GetSection(TrainingAccessOptions.SectionName));
builder.Services.AddScoped<TrainingAccessCodeService>();
builder.Services.AddScoped<TrainingParticipantService>();
builder.Services.AddScoped<TrainingAssignmentService>();
builder.Services.AddScoped<TrainingInvitationService>();
builder.Services.AddScoped<TrainingParticipantSessionService>();
builder.Services.AddScoped<TrainingParticipantPortalService>();
builder.Services.AddScoped<ITrainingCertificatePdfService, TrainingCertificatePdfService>();
builder.Services.AddScoped<TrainingCertificateService>();

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

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(60);
});

// Identity-Stub für generische Identity-UI; Passwortreset nutzt IEmailService direkt.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

DataProtectionStartupLogger.LogEffectiveConfiguration(app);

var uploadPath = app.Configuration["Storage:UploadPath"] ?? "Data/Uploads";
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, uploadPath));

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
app.UseProvisioningRedirects();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantInitialization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Mandantenwechsel per HTTP-Request (Session vor Response-Start). Blazor-Komponenten leiten hierher um.
app.MapGet("/tenant/switch/{tenantId:int}", TenantSwitchEndpoints.SwitchTenantAsync)
    .RequireAuthorization();

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

    return await TenantSwitchEndpoints.SwitchTenantAsync(
        tenantId,
        tenantService,
        context,
        context.Request.Form["returnUrl"].FirstOrDefault());
})
.RequireAuthorization();

app.MapGet("/platform/support-access/enter/{grantId:int}", SupportAccessEndpoints.EnterSupportModeAsync)
    .RequireAuthorization();

app.MapGet("/platform/support-access/exit", SupportAccessEndpoints.ExitSupportModeAsync)
    .RequireAuthorization();

// Minimal-API-Endpunkte für Identity-Formulare (Logout, externe Logins, …).
app.MapAdditionalIdentityEndpoints();
app.MapDocumentFileEndpoints();
app.MapTrainingAssetEndpoints();
app.MapTrainingParticipantAssetEndpoints();
app.MapTrainingParticipantLoginEndpoints();
app.MapTrainingParticipantCertificateEndpoints();
app.MapTenantDataEndpoints();
app.MapLegalDocumentEndpoints();

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();
