using Microsoft.Agents.AI;

namespace AsmtAI.Infrastructure.Ai;

public interface IAssessmentAgentProvider
{
    string Name { get; }
    AIAgent CreateAgent();
}