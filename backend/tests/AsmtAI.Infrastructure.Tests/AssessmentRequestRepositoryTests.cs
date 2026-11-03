using AsmtAI.Application.Requests;
using AsmtAI.Domain.Requests;
using AsmtAI.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Tests;

public sealed class AssessmentRequestRepositoryTests
{
    [Fact]
    public async Task SavesRequestAndExpertReviewAcrossScopes()
    {
        using var provider = new ServiceCollection().AddAssessmentPersistence().BuildServiceProvider();
        var request = new AssessmentRequest("Demo applicant", "A3", "slides.pptx", [1, 2, 3]);
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAssessmentRequestRepository>().AddAsync(request, CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAssessmentRequestRepository>();
            var stored = await repository.FindAsync(request.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal(new byte[] { 1, 2, 3 }, stored.Presentation);
            stored.SaveReview("Question", "Transcript", "Feedback", true);
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAssessmentRequestRepository>();
            var stored = Assert.Single(await repository.ListAsync(CancellationToken.None));
            Assert.Equal("Question", stored.Questions);
            Assert.Equal("Transcript", stored.Transcript);
            Assert.Equal("Feedback", stored.Feedback);
            Assert.True(stored.Reviewed);
        }
    }
}