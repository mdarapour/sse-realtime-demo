using MongoDB.Driver;
using SseDemo.Auth;
using SseDemo.Middleware;
using SseDemo.Outbox;
using SseDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to allow synchronous I/O operations
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: {key}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add authentication
builder.Services.AddAuthentication()
    .AddApiKeyAuthentication();

builder.Services.AddAuthorization();

// Configure MongoDB (required for distributed SSE)
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] 
    ?? throw new InvalidOperationException("MongoDB:ConnectionString is required");

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));

// Register SseService as singleton - IMPORTANT: Only register once!
builder.Services.AddSingleton<SseService>();
builder.Services.AddSingleton<ISseService>(sp => sp.GetRequiredService<SseService>());

// Register outbox service
builder.Services.AddSingleton<SseOutboxService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SseOutboxService>());

// Register SSE message service
builder.Services.AddSingleton<SseMessageService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddMongoDb(
        clientFactory: sp => sp.GetRequiredService<IMongoClient>(),
        name: "mongodb",
        tags: new[] { "ready" });

// Add CORS policy for the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? 
            new[] { "http://localhost:5173" };
        
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Add authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

// Use SSE middleware after authentication
app.UseSse();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Ensure proper disposal of SseService on shutdown
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    var sseService = app.Services.GetService<SseService>();
    if (sseService is IDisposable disposable)
    {
        disposable.Dispose();
    }
});

app.Run();
