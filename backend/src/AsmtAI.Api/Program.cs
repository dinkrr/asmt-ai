using AsmtAI.Api.Endpoints;
using AsmtAI.Infrastructure.Ai;
using AsmtAI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAssessmentPersistence();
builder.Services.AddAssessmentAi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapRequestEndpoints();
}

app.MapHealthEndpoints();

app.Run();

public partial class Program { }
