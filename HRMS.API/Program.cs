using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using FluentValidation;
using AutoMapper;
using HRMS.API.Configuration;
using HRMS.API.Middleware;
using HRMS.API.Services.Interfaces;
using HRMS.API.Services;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Repositories;
using HRMS.AI.Services.Interface;
using HRMS.AI.Services;

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
});

// -------------------- CLOUDINARY --------------------
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));

builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<ICloudinaryService>(sp => sp.GetRequiredService<CloudinaryService>());
builder.Services.AddScoped<IFileUploadService>(sp => sp.GetRequiredService<CloudinaryService>());

// -------------------- CORS --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// -------------------- HTTP CLIENTS --------------------
builder.Services.AddHttpClient();

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
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatBoardStatsService, ChatBoardStatsService>();
builder.Services.AddScoped<IIntentRecognitionService, IntentRecognitionService>();
builder.Services.AddScoped<IGuidanceAIService, GuidanceAIService>();
builder.Services.AddScoped<IChatBoardAiService, ChatBoardAiService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IJobsService, JobsService>();

// -------------------- REPOSITORIES --------------------
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IChatBoardAiRepository, ChatBoardAiRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IJobsRepository, JobsRepository>();

// -------------------- HOSTED SERVICES --------------------
builder.Services.AddHostedService<LeaveBalanceResetHostedService>();

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
