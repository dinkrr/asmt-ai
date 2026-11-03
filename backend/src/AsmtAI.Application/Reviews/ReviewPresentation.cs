using AsmtAI.Domain.Reviews;

namespace AsmtAI.Application.Reviews;

public sealed class ReviewPresentation(IAssessmentAssistant assistant)
{
    public Task<ReviewDraft> ExecuteAsync(string presentation, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presentation);
        return assistant.GenerateReviewAsync(presentation, cancellationToken);
    }
}