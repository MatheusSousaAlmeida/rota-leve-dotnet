using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RotaLeve.Api.Infrastructure.Data;
using RotaLeve.Api.Infrastructure.Errors;
using RotaLeve.Api.Infrastructure.IoC;
using RotaLeve.Api.Infrastructure.Security;
using Serilog;
using Serilog.Context;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "RotaLeve.API")
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [CorrelationId:{CorrelationId}] [TraceId:{TraceId}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services.AddSerilog();
Log.Information("Logging estruturado inicializado. Arquivos de log em {LogDirectory}", logDirectory);

builder.Services.AddRotaLeveInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();

var secret = builder.Configuration["Secretkey"] ?? throw new InvalidOperationException("Secretkey não configurada.");
var key = JwtKeyHelper.Create(secret);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rota Leve API",
        Version = "v1",
        Description = "API colaborativa de acessibilidade urbana do projeto CitizenTech Rota Leve."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe o token JWT."
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

builder.Services.AddRateLimiter(options =>
{
    static string Partition(HttpContext c) =>
        c.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? c.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    options.AddPolicy("relatos", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            Partition(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("confirmacoes", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            Partition(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("API em execução."),
        tags: ["live"])
    .AddOracle(
        builder.Configuration.GetConnectionString("OracleDbConnection") ?? string.Empty,
        name: "oracle",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
    const string correlationHeader = "X-Correlation-ID";
    var correlationId = context.Request.Headers[correlationHeader].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString("N");

    context.TraceIdentifier = correlationId;
    context.Response.Headers[correlationHeader] = correlationId;

    var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("TraceId", traceId))
    {
        await next();
    }
});

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
    {
        if (exception is not null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        return httpContext.Response.StatusCode >= 400
            ? LogEventLevel.Warning
            : LogEventLevel.Information;
    };
});

app.UseExceptionHandler();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

    const int maxRetries = 6;
    const int retryDelaySeconds = 20;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            dbContext.Database.Migrate();
            Log.Information("Migrations do banco de dados aplicadas/verificadas com sucesso");
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            Log.Warning(ex,
                "Tentativa {Attempt}/{Max} de aplicar migrations falhou. Nova tentativa em {Delay}s...",
                attempt, maxRetries, retryDelaySeconds);
            Thread.Sleep(TimeSpan.FromSeconds(retryDelaySeconds));
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Todas as {Max} tentativas de aplicar migrations falharam. A API iniciara sem garantia de schema atualizado.",
                maxRetries);
        }
    }
}

app.Run();

public partial class Program { }
