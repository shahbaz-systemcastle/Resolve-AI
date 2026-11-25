using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Interfaces.Phase4;
using resolve_AI.Application.Services;
using resolve_AI.Application.Services.Phase4;
using resolve_AI.Application.UseCases.PasswordExpired;
using resolve_AI.Infrastructure.Services;
using resolve_AI.Infrastructure.Services.Phase4;
using DotNetEnv;

// Load .env file for Azure credentials. Support both the repository root
// and the project directory so local runs work from either folder.
var envPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")
};

foreach (var envPath in envPaths.Where(File.Exists))
    DotNetEnv.Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();

builder.Services.AddScoped<IPhase7WorkflowService, Phase7WorkflowService>();

// Real Microsoft Graph service only. Without AZURE_* settings it fails with a
// clear error on first use, instead of silently answering with fake data.
builder.Services.AddScoped<IMicrosoft365IntegrationService, Microsoft365IntegrationService>();

builder.Services.AddScoped<PasswordExpiredWorkflow>();

builder.Services.AddScoped<IPasswordService, MockPasswordService>();

builder.Services.AddScoped<IExchangeIntegrationService, ExchangeIntegrationService>();

// WBS 7.13b: logs alerts only until a notification channel is confirmed.
builder.Services.AddScoped<IMailboxAlertNotifier, GraphMailboxAlertNotifier>();

// WBS 7.14: logs the approved action only until the real action is defined.
builder.Services.AddScoped<IMailboxActionExecutor, ManualMailboxActionExecutor>();

// WBS 7.17: orchestration handoff validation and logging.
builder.Services.AddScoped<IPhase7HandoffService, Phase7HandoffService>();

// WBS 8.12-8.22: Phase 8 - Result and Logging Module.
// Stores requests, results, and audit logs for compliance and reporting.
builder.Services.AddScoped<IPhase8StorageService, Phase8StorageService>();

// WBS 4.01-4.12: Phase 4 - Automation Orchestration Module.
// Routes requests to appropriate phases (5, 6, or 7) and collects results.
builder.Services.AddScoped<IPhase4OrchestrationService, Phase4OrchestrationService>();
builder.Services.AddScoped<IPhase4RoutingResolver, Phase4RoutingResolver>();
builder.Services.AddScoped<IPhase4ResultCollector, Phase4ResultCollector>();
builder.Services.AddScoped<IPhase4ErrorHandler, Phase4ErrorHandler>();
builder.Services.AddScoped<IPhase5ExecutionProxy, Phase5ExecutionProxy>();
builder.Services.AddScoped<IPhase6ExecutionProxy, Phase6ExecutionProxy>();
builder.Services.AddScoped<IPhase7ExecutionProxy, Phase7ExecutionProxy>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

