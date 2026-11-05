using AutoMapper;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using HRMS.AI.Services;
using HRMS.AI.Services.Interface;
using HRMS.API.Authorization;
using HRMS.API.Configuration;
using HRMS.API.Hubs;
using HRMS.API.Middleware;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using HRMS.API.Services.PayslipLayouts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using static HRMS.API.Repositories.OnboardingRepository;

var builder = WebApplication.CreateBuilder(args);

// Helper to convert Railway's postgresql:// URL to standard .NET Connection String
static string ParseRailwayUrl(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString) || (!connectionString.StartsWith("postgres://") && !connectionString.StartsWith("postgresql://")))
        return connectionString ?? string.Empty;

    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    return $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Prefer;Trust Server Certificate=true;";
}

// ===================== RAILWAY PORT FIX =====================
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ===========================================================

// -------------------- SERILOG --------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/hrms-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// -------------------- HTTP CONTEXT --------------------
// Required by DatabaseConnectionFactory to inject the logged-in User ID
// into the PostgreSQL session for audit trigger support.
builder.Services.AddHttpContextAccessor();

// -------------------- CONTROLLERS --------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Emit every DateTime as UTC ISO-8601 with 'Z' so the frontend reliably
        // converts timestamps into the company-configured timezone.
        options.JsonSerializerOptions.Converters.Add(new HRMS.API.Configuration.UtcJsonDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new HRMS.API.Configuration.UtcJsonNullableDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new HRMS.API.Configuration.UtcJsonDateTimeOffsetConverter());
        options.JsonSerializerOptions.Converters.Add(new HRMS.API.Configuration.UtcJsonNullableDateTimeOffsetConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// -------------------- SWAGGER --------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "HRMS API",
        Version = "v1",
        Description = "Human Resource Management System API"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------- DAPPER --------------------
DapperConfiguration.Configure();

// -------------------- DATABASE --------------------
// Resolve and validate the connection string once at startup.
// The CONNECTION_STRING environment variable wins over local configuration, and is required in production.
var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(envConnectionString))
    throw new InvalidOperationException("CONNECTION_STRING must be configured for production.");

var rawConnectionString = !string.IsNullOrWhiteSpace(envConnectionString)
    ? envConnectionString
    : builder.Configuration.GetConnectionString("DefaultConnection")
      ?? Environment.GetEnvironmentVariable("DATABASE_URL");

var finalConnectionString = ParseRailwayUrl(rawConnectionString);

if (string.IsNullOrEmpty(finalConnectionString))
    throw new InvalidOperationException("Database connection string not found.");
Console.WriteLine($"DB source: {(string.IsNullOrWhiteSpace(envConnectionString) ? "local configuration" : "CONNECTION_STRING environment variable")}");

// Use AddScoped (not Singleton) because DatabaseConnectionFactory now depends on
// IHttpContextAccessor which is request-scoped. A Singleton cannot safely consume
// a Scoped dependency.
builder.Services.AddScoped<IDatabaseConnectionFactory>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new DatabaseConnectionFactory(finalConnectionString, httpContextAccessor);
});

// -------------------- JWT --------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var jwtSecret = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("JwtSettings:SecretKey must be configured with at least 32 bytes.");

if (builder.Environment.IsProduction())
{
    var requiredSecrets = new[]
    {
        "MailJet:PublicKey",
        "MailJet:PrivateKey",
        "Cloudinary:ApiKey",
        "Cloudinary:ApiSecret"
    };
    var missingSecrets = requiredSecrets
        .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
        .ToArray();

    var hasGroqApiKey = builder.Configuration.GetSection("GroqAI:ApiKeys")
                            .GetChildren()
                            .Any(key => !string.IsNullOrWhiteSpace(key.Value))
                        || !string.IsNullOrWhiteSpace(builder.Configuration["GroqAI:ApiKey"])
                        || !string.IsNullOrWhiteSpace(builder.Configuration["GROQ_API_KEY"]);

    if (!hasGroqApiKey)
        missingSecrets = missingSecrets.Append("GroqAI:ApiKeys:0 (or GROQ_API_KEY)").ToArray();

    if (missingSecrets.Length > 0)
        throw new InvalidOperationException(
            $"Missing required production settings: {string.Join(", ", missingSecrets)}");
}

var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var requestPath = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                requestPath.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// -------------------- CLOUDINARY --------------------
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<ICloudinaryService>(sp => sp.GetRequiredService<CloudinaryService>());
builder.Services.AddScoped<IFileUploadService>(sp => sp.GetRequiredService<CloudinaryService>());

// -------------------- SIGNALR & HANGFIRE --------------------
builder.Services.AddSignalR();

// Same database as the app (CONNECTION_STRING first). Reading appsettings first made Hangfire
// connect to the "localhost" placeholder inside the container and crash the API at startup.
var hangfireConnectionString = finalConnectionString;
if (string.IsNullOrEmpty(hangfireConnectionString))
    throw new Exception("Database connection string not found for Hangfire");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnectionString)));

builder.Services.AddHangfireServer();

// -------------------- CORS --------------------
var allowedOrigins = builder.Configuration
    .GetSection("CORS:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    throw new InvalidOperationException("CORS:AllowedOrigins must contain the production frontend origins.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(
                    origin,
                    UriKind.Absolute,
                    out var uri))
                {
                    return false;
                }

                var isConfiguredOrigin = allowedOrigins.Any(allowedOrigin =>
                    string.Equals(allowedOrigin, origin, StringComparison.OrdinalIgnoreCase));

                var isBriskPeopleOrigin = uri.Scheme == Uri.UriSchemeHttps &&
                    (uri.Host.Equals(
                        "briskpeople.com",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    uri.Host.EndsWith(
                        ".briskpeople.com",
                        StringComparison.OrdinalIgnoreCase));

                var isLocalDevelopmentOrigin = builder.Environment.IsDevelopment() &&
                    uri.Scheme == Uri.UriSchemeHttp &&
                    (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                     uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));

                return isConfiguredOrigin || isBriskPeopleOrigin || isLocalDevelopmentOrigin;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // SignalR negotiates with credentials
    });
});

// -------------------- HTTP CLIENTS --------------------
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<GuidanceAIService>(client =>
{
    var url = builder.Configuration["GroqAI:Url"];
    if (!string.IsNullOrEmpty(url))
        client.BaseAddress = new Uri(url);
});

builder.Services.AddHttpClient<IntentRecognitionService>(client =>
{
    var url = builder.Configuration["GroqAI:Url"];
    if (!string.IsNullOrEmpty(url))
        client.BaseAddress = new Uri(url);
});

// -------------------- AUTOMAPPER & VALIDATION --------------------
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// -------------------- SERVICES --------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IDomainService, DomainService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IIpAddressService, IpAddressService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPayslipTemplateService, PayslipTemplateService>();
builder.Services.AddScoped<IPayslipPdfBuilder, PayslipPdfBuilder>();
builder.Services.AddScoped<IPayslipLayoutRendererRegistry, PayslipLayoutRendererRegistry>();
builder.Services.AddScoped<IPayslipLayoutRenderer, ClassicStackedLayoutRenderer>();
builder.Services.AddScoped<IPayslipLayoutRenderer, SplitColumnsLayoutRenderer>();
builder.Services.AddScoped<IPayslipLayoutRenderer, CompactCorporateLayoutRenderer>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatBoardStatsService, ChatBoardStatsService>();
builder.Services.AddScoped<IIntentRecognitionService, IntentRecognitionService>();
builder.Services.AddScoped<IGuidanceAIService, GuidanceAIService>();
builder.Services.AddScoped<IChatBoardAiService, ChatBoardAiService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IJobsService, JobsService>();
builder.Services.AddScoped<ICareerService, CareerService>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddHttpClient<IResumeParserService, ResumeParserService>(client => 
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IPayRollService, PayRollService>();
builder.Services.AddScoped<IPayslipBulkUploadService, PayslipBulkUploadService>();
builder.Services.AddScoped<IBulkPayslipJobService, BulkPayslipJobService>();
// Singleton: Groq keys are validated once for the whole app, not on every chat request.
builder.Services.AddSingleton<IApiKeyProvider, ApiKeyProvider>();
builder.Services.AddScoped<IHelpDeskService, HelpDeskService>();
builder.Services.AddScoped<IGeoFenceService, GeoFenceService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IOnboardingConfigService, OnboardingConfigService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();


// --- Super Admin Portal Services ---
builder.Services.AddScoped<IPlatformAdminAuthService, PlatformAdminAuthService>();
builder.Services.AddScoped<IDemoInquiryService, DemoInquiryService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IEntitlementRepository, EntitlementRepository>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();

// -------------------- REPOSITORIES --------------------
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<ITimesheetConfigRepository, TimesheetConfigRepository>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ITimesheetWorkflowRepository, TimesheetWorkflowRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<HRMS.API.Authorization.ITenantGuard, HRMS.API.Authorization.TenantGuard>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IPayslipTemplateRepository, PayslipTemplateRepository>();
builder.Services.AddScoped<IPayslipLayoutCatalogRepository, PayslipLayoutCatalogRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IChatBoardAiRepository, ChatBoardAiRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IJobsRepository, JobsRepository>();
builder.Services.AddScoped<ICareerRepository, CareerRepository>();
builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
builder.Services.AddScoped<IPayRollRepository, PayRollRepository>();
builder.Services.AddScoped<IHelpDeskRepository, HelpDeskRepository>();
builder.Services.AddScoped<IGeoFenceRepository, GeoFenceRepository>();
builder.Services.AddScoped<IOnboardingRepository, OnboardingRepository>();
// In Program.cs / Startup.cs — add these two lines with your other DI registrations:
builder.Services.AddScoped<IOnboardingConfigRepository, OnboardingConfigRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();

// --- Super Admin Portal Repositories ---
builder.Services.AddScoped<IPlatformAdminRepository, PlatformAdminRepository>();
builder.Services.AddScoped<IDemoInquiryRepository, DemoInquiryRepository>();
builder.Services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

// --- Audit Log Repository ---
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();


// -------------------- HOSTED SERVICES --------------------
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
// -------------------- HOSTED SERVICES --------------------
builder.Services.AddHostedService<LeaveBalanceResetHostedService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200_000_000; // 200MB
});
// ===================== PIPELINE =====================
var app = builder.Build();

// Validate the Groq key once at startup in the background, so the first chat message doesn't wait for it
// and a bad key is logged right away instead of failing silently later.
_ = Task.Run(() =>
{
    try { app.Services.GetRequiredService<IApiKeyProvider>().GetCurrentKey(); }
    catch (Exception ex) { app.Logger.LogError(ex, "AI Assistant is unavailable: Groq API key check failed at startup."); }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS API V1");
    c.RoutePrefix = string.Empty;
});


app.UseStaticFiles();

// ❌ REMOVED HTTPS REDIRECTION (Railway handles HTTPS)

// IMPORTANT ORDER
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantEntitlementMiddleware>();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

app.MapHub<PayslipBulkHub>("/hubs/payslip-bulk");
app.MapHub<PermissionHub>("/hubs/permissionHub");
app.MapHub<HRMS.API.Hubs.CvParsingHub>("/hub/cvparsing");
app.MapControllers();

try
{
    Log.Information("🚀 HRMS API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application crashed");
}
finally
{
    Log.CloseAndFlush();
}

// Exposes the implicit Program class (top-level statements) to the test project
// so WebApplicationFactory<Program> can boot the real app in integration tests.
public partial class Program { }
