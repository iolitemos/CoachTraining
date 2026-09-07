using System.Text;
using System.Text.Json.Serialization;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Middleware;
using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging ----------
// Structured (JSON) console logging so log entries are machine-parseable in every environment.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = false;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// ---------- Services ----------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Standardize automatic [ApiController] model validation (400) responses.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
                    new ApiFieldError(kvp.Key, e.ErrorMessage)))
                .ToList();

            var response = new ApiErrorResponse("Validation failed", errors);
            return new BadRequestObjectResult(response);
        };
    })
    // Enums (SessionStatus, TrainingType, AttendanceStatus, ...) serialize as
    // their string names, not raw ints — much more usable for the frontend and logs.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---------- Authentication (todo.md section 3) ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<IAthleteService, AthleteService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ISessionStatusService, SessionStatusService>();
builder.Services.AddScoped<IScheduleConflictService, ScheduleConflictService>();
builder.Services.AddScoped<IRoutineScheduleService, RoutineScheduleService>();
builder.Services.AddScoped<IPrivateSessionService, PrivateSessionService>();
builder.Services.AddScoped<ITrainingSessionService, TrainingSessionService>();
builder.Services.AddScoped<ICoachTeachingService, CoachTeachingService>();
builder.Services.AddScoped<IRoutineAttendanceService, RoutineAttendanceService>();
builder.Services.AddScoped<IPrivateAttendanceService, PrivateAttendanceService>();
builder.Services.AddScoped<ITrainingLogService, TrainingLogService>();

// PostgreSQL connection string is supplied via appsettings.{Environment}.json for
// non-secret defaults and overridden by the ConnectionStrings__Default environment
// variable (or user-secrets in Development) for actual credentials.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health-check endpoint for deployment validation (DEV/QAS/PROD).
app.MapHealthChecks("/health");

// Seed roles + a default Administrator account so the system is usable before
// any user is manually created. Idempotent; safe to run on every startup.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();
    var seedLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    await DbSeeder.SeedAsync(db, passwordHasher, app.Configuration, seedLogger);
}

app.Run();
