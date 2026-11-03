using AsmtAI.Api.Contracts;
using AsmtAI.Application.Requests;
using AsmtAI.Domain.Requests;

namespace AsmtAI.Api.Services;

public sealed class AssessmentRequestService(IAssessmentRequestRepository repository) : IAssessmentRequestService
{
    public async Task<IReadOnlyList<AssessmentRequestSummary>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(ToSummary).ToArray();

    public async Task<AssessmentRequestSummary> CreateAsync(string candidateName, string targetLevel, string fileName, byte[] presentation, CancellationToken cancellationToken)
    {
        var request = new AssessmentRequest(candidateName, targetLevel, fileName, presentation);
        await repository.AddAsync(request, cancellationToken);
        return ToSummary(request);
    }

    public async Task<AssessmentRequestDetail?> GetAsync(Guid id, bool expert, CancellationToken cancellationToken)
    {
        var request = await repository.FindAsync(id, cancellationToken);
        return request is null ? null : new AssessmentRequestDetail(ToSummary(request), expert ? request.Questions : null,
            expert ? request.Transcript : null, expert ? request.Feedback : null);
    }

    public async Task<PresentationFile?> GetPresentationAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await repository.FindAsync(id, cancellationToken);
        return request is null ? null : new PresentationFile(request.PresentationName, request.Presentation);
    }

    public async Task<bool> SaveReviewAsync(Guid id, SaveAssessmentReview input, CancellationToken cancellationToken)
    {
        var request = await repository.FindAsync(id, cancellationToken);
        if (request is null) return false;
        request.SaveReview(input.Questions ?? string.Empty, input.Transcript ?? string.Empty, input.Feedback ?? string.Empty, input.Reviewed);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AssessmentRequestSummary ToSummary(AssessmentRequest request) =>
        new(request.Id, request.CandidateName, request.TargetLevel, request.PresentationName, request.CreatedAt, request.Reviewed);
}