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

    [Fact]
    public void CopilotProviderRequiresAgentRepositoryConfiguration()
    {
        using var provider = CreateServices("copilot").BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ReviewPresentation>());
        Assert.Contains("Ai:AgentRepositoryPath", exception.Message);
    }

    [Fact]
    public void CopilotProviderReportsMissingDefinitionsBeforeStartingClient()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:Provider"] = "copilot",
            ["Ai:AgentRepositoryPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        }).Build();
        using var provider = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddAssessmentAi().BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ReviewPresentation>());
        Assert.Contains("unified-assessment-agent.md", exception.Message);
    }

    [Fact]
    public async Task CopilotProviderCreatesAnAgentFromConfiguredDefinitions()
    {
        var root = Path.Combine(Path.GetTempPath(), $"asmt-ai-{Guid.NewGuid()}");
        string[] paths =
        [
            ".github/agents/unified-assessment-agent.md",
            ".github/skills/review-self-presentation/SKILL.md",
            ".github/skills/review-self-presentation/references/level-up-requirements-dotnet.md",
            ".github/skills/review-self-presentation/references/level-up-skill-proficiency-descriptions.md"
        ];
        try
        {
            foreach (var path in paths)
            {
                var file = Path.Combine(root, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                await File.WriteAllTextAsync(file, path);
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "copilot",
                ["Ai:AgentRepositoryPath"] = root
            }).Build();
            await using var provider = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddAssessmentAi().BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ReviewPresentation>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [LiveCopilotFact]
    public async Task CopilotCanReviewSyntheticPresentationWhenEnabled()
    {
        var root = Environment.GetEnvironmentVariable("ASMT_AI_LIVE_AGENT_REPOSITORY");

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:Provider"] = "copilot",
            ["Ai:AgentRepositoryPath"] = root
        }).Build();
        await using var provider = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddAssessmentAi().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var review = await scope.ServiceProvider.GetRequiredService<ReviewPresentation>().ExecuteAsync(
            "Synthetic candidate: Demo Person. Target title: A3. Presentation: Built a C# API with tests and mentored a teammate.", cancellation.Token);
        Assert.False(string.IsNullOrWhiteSpace(review.Content));
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

public sealed class LiveCopilotFactAttribute : FactAttribute
{
    public LiveCopilotFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASMT_AI_LIVE_AGENT_REPOSITORY")))
            Skip = "Set ASMT_AI_LIVE_AGENT_REPOSITORY to enable the live Copilot test.";
    }
}