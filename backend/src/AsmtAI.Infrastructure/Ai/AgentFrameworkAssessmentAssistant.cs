using AsmtAI.Application.Reviews;
using AsmtAI.Domain.Reviews;
using Microsoft.Agents.AI;

namespace AsmtAI.Infrastructure.Ai;

public sealed class AgentFrameworkAssessmentAssistant(AIAgent agent) : IAssessmentAssistant
{
    public async Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken)
    {
        var response = await agent.RunAsync(presentation, cancellationToken: cancellationToken);
        return new ReviewDraft(response.ToString());
    }
}