using AsmtAI.Application.Reviews;
using GitHub.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Ai;

public static class AssessmentAgentRegistration
{
    public static IServiceCollection AddAssessmentAi(this IServiceCollection services)
    {
        services.AddSingleton<CopilotClient>();
        services.AddScoped<IAssessmentAgentProvider, CopilotAssessmentAgentProvider>();
        services.AddScoped<AssessmentAgentProviderSelector>();
        services.AddScoped(provider => provider.GetRequiredService<AssessmentAgentProviderSelector>().Select().CreateAgent());
        services.AddScoped<IAssessmentAssistant, AgentFrameworkAssessmentAssistant>();
        services.AddScoped<ReviewPresentation>();
        return services;
    }
}