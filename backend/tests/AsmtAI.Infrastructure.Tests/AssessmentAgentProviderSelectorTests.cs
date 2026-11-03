using AsmtAI.Application.Reviews;
using AsmtAI.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Tests;

public sealed class AssessmentAgentProviderSelectorTests
{
    [Fact]
    public void SelectsConfiguredProviderAndUsesItWhenResolvingAssistant()
    {
        var first = new TestProvider("first");
        var second = new TestProvider("second");
        using var provider = CreateServices("SECOND", first, second).BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Same(second, scope.ServiceProvider.GetRequiredService<AssessmentAgentProviderSelector>().Select());
        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ReviewPresentation>());
        Assert.Equal("Created second", exception.Message);
    }

    [Fact]
    public void DefaultsToAgentFrameworkProvider()
    {
        var strategy = new TestProvider("agent-framework");
        using var provider = CreateServices(null, strategy).BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Same(strategy, scope.ServiceProvider.GetRequiredService<AssessmentAgentProviderSelector>().Select());
    }

    [Fact]
    public void ReportsUnregisteredProvider()
    {
        using var provider = CreateServices("missing", new TestProvider("first")).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ReviewPresentation>());
        Assert.Contains("AI provider 'missing' is not registered", exception.Message);
    }

    [Fact]
    public void RejectsDuplicateProviderNamesIgnoringCase()
    {
        using var provider = CreateServices("first", new TestProvider("first"), new TestProvider("FIRST")).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AssessmentAgentProviderSelector>().Select());
        Assert.Contains("Multiple AI providers", exception.Message);
    }

    private static IServiceCollection CreateServices(string? name, params IAssessmentAgentProvider[] providers)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:Provider"] = name }).Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration);
        foreach (var provider in providers)
        {
            services.AddSingleton(provider);
        }

        return services.AddAssessmentAi();
    }

    private sealed class TestProvider(string name) : IAssessmentAgentProvider
    {
        public string Name => name;
        public AIAgent CreateAgent() => throw new InvalidOperationException($"Created {name}");
    }
}