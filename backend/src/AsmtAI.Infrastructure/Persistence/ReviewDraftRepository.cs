using AsmtAI.Application.Reviews;
using AsmtAI.Domain.Reviews;

namespace AsmtAI.Infrastructure.Persistence;

public sealed class ReviewDraftRepository(AssessmentDbContext context) : IReviewDraftRepository
{
    public async Task<Guid> AddAsync(ReviewDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var entry = new ReviewDraftEntry { Id = Guid.NewGuid(), Content = draft.Content };
        context.ReviewDrafts.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task<ReviewDraft?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await context.ReviewDrafts.FindAsync([id], cancellationToken);
        return entry is null ? null : new ReviewDraft(entry.Content);
    }
}