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
builder.Services.AddSingleton<IDatabaseConnectionFactory>(_ =>
{
    var envConnection = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    var configConnection = builder.Configuration.GetConnectionString("DefaultConnection");

    var finalConnection = !string.IsNullOrEmpty(envConnection)
        ? envConnection
        : configConnection;

    if (string.IsNullOrEmpty(finalConnection))
        throw new Exception("❌ Database connection string not found");

    Console.WriteLine($"✅ DB Source: {(envConnection != null ? "Railway ENV" : "Local Config")}");

    return new DatabaseConnectionFactory(finalConnection);
});

// -------------------- JWT --------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
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

var hangfireConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string not found for Hangfire");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnectionString)));

builder.Services.AddHangfireServer();

// -------------------- CORS --------------------
var configuredOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
var allowedHostSuffixes = builder.Configuration.GetSection("CORS:AllowedHostSuffixes").Get<string[]>()
    ?? new[] { ".localhost", ".briskpeople.com", ".vercel.app" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => CorsOriginHelper.IsOriginAllowed(origin, configuredOrigins, allowedHostSuffixes))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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
builder.Services.AddScoped<IApiKeyProvider, ApiKeyProvider>();
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

// -------------------- REPOSITORIES --------------------
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IIpAddressRepository, IpAddressRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
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

// -------------------- HOSTED SERVICES --------------------
builder.Services.AddAuthorization();
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS API V1");
    c.RoutePrefix = string.Empty;
});


app.UseStaticFiles();

// ❌ REMOVED HTTPS REDIRECTION (Railway handles HTTPS)

// IMPORTANT ORDER
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

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
