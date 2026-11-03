using AsmtAI.Api.Services;
using AsmtAI.Api.Filters;
using AsmtAI.Infrastructure.Ai;
using AsmtAI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true)
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new LocalDemoControllerFeatureProvider(builder.Environment)));
builder.Services.AddAssessmentPersistence();
builder.Services.AddAssessmentAi();
builder.Services.AddScoped<IAssessmentRequestService, AssessmentRequestService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

public partial class Program { }
