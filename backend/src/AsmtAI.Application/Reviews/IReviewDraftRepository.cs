using AsmtAI.Domain.Reviews;

namespace AsmtAI.Application.Reviews;

public interface IReviewDraftRepository
{
    Task<Guid> AddAsync(ReviewDraft draft, CancellationToken cancellationToken);
    Task<ReviewDraft?> FindAsync(Guid id, CancellationToken cancellationToken);
}