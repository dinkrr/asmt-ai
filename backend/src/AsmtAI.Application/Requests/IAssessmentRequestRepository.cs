using AsmtAI.Domain.Requests;

namespace AsmtAI.Application.Requests;

public interface IAssessmentRequestRepository
{
    Task<IReadOnlyList<AssessmentRequest>> ListAsync(CancellationToken cancellationToken);
    Task<AssessmentRequest?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(AssessmentRequest request, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}