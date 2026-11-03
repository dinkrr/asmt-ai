using AsmtAI.Domain.Reviews;

namespace AsmtAI.Application.Reviews;

public interface IAssessmentAssistant
{
    Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken);
}