using AsmtAI.Application.Requests;
using AsmtAI.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace AsmtAI.Infrastructure.Persistence;

public sealed class AssessmentRequestRepository(AssessmentDbContext context) : IAssessmentRequestRepository
{
    public async Task<IReadOnlyList<AssessmentRequest>> ListAsync(CancellationToken cancellationToken) =>
        await context.Requests.AsNoTracking().OrderByDescending(request => request.CreatedAt).ToListAsync(cancellationToken);

    public async Task<AssessmentRequest?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Requests.FindAsync([id], cancellationToken);

    public async Task AddAsync(AssessmentRequest request, CancellationToken cancellationToken)
    {
        context.Requests.Add(request);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);
}