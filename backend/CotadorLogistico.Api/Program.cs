using System.Threading.RateLimiting;
using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Middleware;
using CotadorLogistico.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    LoadDotEnvFile(Path.Combine(AppContext.BaseDirectory, ".env"));
    LoadDotEnvFile(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()

        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddInMemoryCollection(BuildFlatEnvironmentVariableMap());

    builder.Services.AddCotadorInfrastructure(builder.Configuration);
    builder.Services.AddSupabaseJwtAuthentication();
    builder.Services.AddAuthorization();

    builder.Services.AddScoped<CurrentUserAccessor>();
    builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
    builder.Services.AddSingleton<CotadorLogistico.Api.Services.IntegrationStatusReader>();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        options.Limits.MaxRequestBodySize = 1_000_000);

    builder.Services.AddRequestTimeouts(options =>
        options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

    var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(CotadorLogistico.Api.RateLimitPolicies.Ai, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirst("sub")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10 }));

        options.AddPolicy(CotadorLogistico.Api.RateLimitPolicies.Demo, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 60 }));

        options.AddPolicy(CotadorLogistico.Api.RateLimitPolicies.Admin, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirst("sub")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 30 }));

        options.AddPolicy(CotadorLogistico.Api.RateLimitPolicies.Shipping, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirst("sub")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 120 }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "global",
                factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromSeconds(10), PermitLimit = 200 }));
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseRequestTimeouts();

    app.UseAuthentication();
    app.UseMiddleware<CurrentUserMiddleware>();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CotadorLogistico.Api encerrou inesperadamente durante a inicialização.");
}
finally
{
    Log.CloseAndFlush();
}

static Dictionary<string, string?> BuildFlatEnvironmentVariableMap()
{
    var map = new Dictionary<string, string?>();

    void Map(string envVarName, string configKey)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(value)) map[configKey] = value;
    }

    Map("SUPABASE_URL", "Supabase:Url");
    Map("SUPABASE_SECRET_KEY", "Supabase:SecretKey");
    Map("SUPABASE_DB_CONNECTION", "Supabase:DbConnectionString");
    Map("SUPABASE_JWT_SECRET", "Supabase:LegacyJwtSecret");

    Map("SECRETS_MODE", "Secrets:Mode");
    Map("SECRETS_MASTER_KEY", "Secrets:MasterKeyBase64");

    Map("GEMINI_MODEL", "Gemini:Model");
    Map("GEMINI_API_BASE_URL", "Gemini:ApiBaseUrl");

    Map("EXCHANGE_RATES_API_BASE_URL", "ExchangeRates:ApiBaseUrl");

    Map("CORS_ALLOWED_ORIGINS", "Cors:AllowedOrigins");

    Map("ALLOWED_EMAIL_DOMAINS", "Security:AllowedEmailDomains");

    return map;
}

static void LoadDotEnvFile(string path)
{
    if (!File.Exists(path)) return;

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0) continue;

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            value = value[1..^1];

        if (Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}
