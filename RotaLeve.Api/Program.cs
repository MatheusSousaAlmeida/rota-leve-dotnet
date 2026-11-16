using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RotaLeve.Api.Infrastructure.Data;
using RotaLeve.Api.Infrastructure.Errors;
using RotaLeve.Api.Infrastructure.IoC;
using RotaLeve.Api.Infrastructure.Health;
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
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
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
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Não registra credenciais nem o conteúdo do token.
            Log.Warning(context.Exception, "Falha ao validar JWT. Caminho: {Path}", context.Request.Path);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT ausente ou recusado em {Path}. Erro: {Error}",
                context.Request.Path, context.Error ?? "token ausente/invalido");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    // Protege endpoints atuais e futuros por padrão. Somente [AllowAnonymous] é público.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rota Leve API",
        Version = "v1",
        Description = "API colaborativa de acessibilidade urbana do projeto CitizenTech Rota Leve."
    });

    // Define o esquema HTTP Bearer e o requisito global de segurança do Swagger.
    var securitySchema = new OpenApiSecurityScheme
    {
        Description = "Informe o JWT retornado pelo login. No esquema HTTP Bearer, cole apenas o token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, Array.Empty<string>() }
    });
});

builder.Services.AddRateLimiter(options =>
{
    static string Partition(HttpContext c) =>
        c.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? c.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    // Limita as consultas de listagem a 5 chamadas por IP a cada 20 segundos.
    options.AddPolicy("listagens", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(20),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

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
        () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API em execução."),
        tags: ["live"])
    .AddCheck<OracleConnectivityHealthCheck>(
        "oracle",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

// Migrations rodam em segundo plano e não bloqueiam Swagger/Health Checks.
// Em testes, não é estabelecida nenhuma conexão com o Oracle.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<OracleMigrationHostedService>();

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


app.Run();

public partial class Program { }
