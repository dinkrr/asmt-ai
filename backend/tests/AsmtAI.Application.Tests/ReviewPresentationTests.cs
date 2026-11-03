using AsmtAI.Application.Reviews;
using AsmtAI.Domain.Reviews;

namespace AsmtAI.Application.Tests;

public sealed class ReviewPresentationTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsEvidenceAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var assistant = new RecordingAssistant();
        var review = new ReviewPresentation(assistant);

        var result = await review.ExecuteAsync("Sample presentation", cancellation.Token);

        Assert.Equal("Sample presentation", assistant.Presentation);
        Assert.Equal(cancellation.Token, assistant.CancellationToken);
        Assert.Equal("Draft", result.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExecuteAsync_RejectsEmptyEvidence(string presentation)
    {
        var review = new ReviewPresentation(new RecordingAssistant());

        Assert.Throws<ArgumentException>(() => { _ = review.ExecuteAsync(presentation, CancellationToken.None); });
    }

    private sealed class RecordingAssistant : IAssessmentAssistant
    {
        public string? Presentation { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken)
        {
            Presentation = presentation;
            CancellationToken = cancellationToken;
            return Task.FromResult(new ReviewDraft("Draft"));
        }
    }
}