using AsmtAI.Api.Contracts;

namespace AsmtAI.Api.Services;

public interface IAssessmentRequestService
{
    Task<IReadOnlyList<AssessmentRequestSummary>> ListAsync(CancellationToken cancellationToken);
    Task<AssessmentRequestSummary> CreateAsync(string candidateName, string targetLevel, string fileName, byte[] presentation, CancellationToken cancellationToken);
    Task<AssessmentRequestDetail?> GetAsync(Guid id, bool expert, CancellationToken cancellationToken);
    Task<PresentationFile?> GetPresentationAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SaveReviewAsync(Guid id, SaveAssessmentReview input, CancellationToken cancellationToken);
}

public sealed record PresentationFile(string FileName, byte[] Content);