using InventoryService.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryService.Business.Services;
using InventoryService.Persistence.Repositories;
using InventoryService.Persistence.UnitOfWork;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using System.Data;
using Npgsql;
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
        .Enrich.WithProperty("Application", "InventoryService")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/inventoryservice-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}");
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Inventory Service API",
        Version = "v1",
        Description = "API for managing inventory items and stock with Enhanced Logging"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

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

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("role", "Admin"));

    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireClaim("role", "User", "Admin"));

    options.AddPolicy("CanManageInventory", policy =>
        policy.RequireClaim("permissions", "inventory:read", "inventory:write"));
});

// Database Connection
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new NpgsqlConnection(connectionString);
});

// Register repositories and services
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInventoryService, InventoryServiceImpl>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Service API V1");
        c.RoutePrefix = string.Empty; // Makes Swagger available at root URL
    });
}

// Enhanced request logging with correlation IDs
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.IncludeQueryInRequestPath = true;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

        // Add correlation ID to diagnostic context
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// Add correlation ID middleware
app.UseCorrelationId();

// Add request/response logging middleware
app.UseRequestResponseLogging();

app.UseHttpsRedirection();
// Important: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting InventoryService on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "InventoryService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}