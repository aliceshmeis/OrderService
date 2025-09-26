using API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.Persistence.Context;
using OrderService.Persistence.Repositories;
using OrderService.Persistence.Repositories.Interfaces;
using OrderService.Persistence.UnitOfWork;
using OrderService.Business.Services;
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
        .Enrich.WithProperty("Application", "OrderService")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/orderservice-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Application}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}");
});

// Add services to the container
builder.Services.AddControllers();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Pattern
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business Services
builder.Services.AddScoped<IOrderService, OrderBusinessService>();
builder.Services.AddScoped<OrderService.API.Services.IJwtService, OrderService.API.Services.JwtService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "Order Management API with JWT Authentication and Enhanced Logging"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

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
//app.UseRequestResponseLogging();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
try
{
    Log.Information("Starting OrderService on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}