using Microsoft.Extensions.Configuration;

namespace AsmtAI.Infrastructure.Ai;

public sealed class AssessmentAgentProviderSelector(
    IConfiguration configuration,
    IEnumerable<IAssessmentAgentProvider> providers)
{
    public IAssessmentAgentProvider Select()
    {
        var name = configuration["Ai:Provider"];
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "agent-framework";
        }

        var matches = providers.Where(provider => string.Equals(provider.Name, name, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"AI provider '{name}' is not registered. Configure Ai:Provider and register its strategy."),
            _ => throw new InvalidOperationException($"Multiple AI providers named '{name}' are registered.")
        };
    }
}