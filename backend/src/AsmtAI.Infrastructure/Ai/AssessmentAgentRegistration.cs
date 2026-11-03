using AsmtAI.Application.Reviews;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Ai;

public static class AssessmentAgentRegistration
{
    public static IServiceCollection AddAssessmentAi(this IServiceCollection services)
    {
        services.AddScoped<AssessmentAgentProviderSelector>();
        services.AddScoped(provider => provider.GetRequiredService<AssessmentAgentProviderSelector>().Select().CreateAgent());
        services.AddScoped<IAssessmentAssistant, AgentFrameworkAssessmentAssistant>();
        services.AddScoped<ReviewPresentation>();
        return services;
    }

    public static IServiceCollection AddAssessmentAgent(this IServiceCollection services, AIAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        services.AddSingleton<IAssessmentAgentProvider>(new InstanceAssessmentAgentProvider(agent));
        return services.AddAssessmentAi();
    }
}