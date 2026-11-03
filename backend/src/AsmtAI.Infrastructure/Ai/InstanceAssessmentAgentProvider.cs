using Microsoft.Agents.AI;

namespace AsmtAI.Infrastructure.Ai;

internal sealed class InstanceAssessmentAgentProvider(AIAgent agent) : IAssessmentAgentProvider
{
    public string Name => "agent-framework";
    public AIAgent CreateAgent() => agent;
}