using resolve_AI.Application.Interfaces;
using resolve_AI.Application.Services;
using resolve_AI.Application.UseCases.PasswordExpired;
using resolve_AI.Infrastructure.Services;
using DotNetEnv;

// Load .env file for Azure credentials
DotNetEnv.Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();

builder.Services.AddScoped<IPhase7WorkflowService, Phase7WorkflowService>();

// Use real service if credentials are configured, otherwise mock
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")))
{
    builder.Services.AddScoped<IMicrosoft365IntegrationService,
        Microsoft365IntegrationService>();
}
else
{
    builder.Services.AddScoped<IMicrosoft365IntegrationService,
        MockMicrosoft365IntegrationService>();
}

builder.Services.AddScoped<PasswordExpiredWorkflow>();

builder.Services.AddScoped<IPasswordService, MockPasswordService>();

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
