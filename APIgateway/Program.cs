using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with enhanced logging and correlation IDs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithCorrelationId()
        .Enrich.WithProperty("Application", "ApiGateway")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/apigateway-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}");
});

// Add configuration for ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Correlation ID services
builder.Services.AddDefaultCorrelationId(options =>
{
    options.CorrelationIdGenerator = () => Guid.NewGuid().ToString("N")[..12]; // 12 character ID
    options.AddToLoggingScope = true;
    options.EnforceHeader = false;
    options.IgnoreRequestHeader = false;
    options.IncludeInResponse = true;
    options.RequestHeader = "X-Correlation-ID";
    options.ResponseHeader = "X-Correlation-ID";
    options.UpdateTraceIdentifier = true;
});

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Enhanced JWT validation events with structured logging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed for {RequestPath}: {ErrorMessage}",
                    context.Request.Path, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("sub")?.Value ?? "Unknown";
                Log.Information("JWT Token validated successfully for user {UserId} on path {RequestPath}",
                    userId, context.Request.Path);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Log.Warning("JWT Challenge triggered for {RequestPath}: {Error}",
                    context.Request.Path, context.Error);
                return Task.CompletedTask;
            }
        };
    });

// Add Ocelot services
builder.Services.AddOcelot();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enhanced request logging with correlation IDs
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "Gateway {RequestMethod} {RequestPath} -> {DownstreamHost} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.IncludeQueryInRequestPath = true;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

        // Add downstream service info if available
        if (httpContext.Items.TryGetValue("OcelotDownstreamRoute", out var downstreamRoute))
        {
            diagnosticContext.Set("DownstreamService", downstreamRoute?.ToString());
        }

        // Add correlation ID to diagnostic context
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// Add correlation ID middleware (must be before Ocelot)
app.UseCorrelationId();

// Enable CORS
app.UseCors("AllowAll");

// Enable Authentication
app.UseAuthentication();

// Use Ocelot middleware (this handles routing and forwarding)
await app.UseOcelot();

Log.Information("API Gateway started successfully on {Environment}", app.Environment.EnvironmentName);
app.Run();