using AsmtAI.Application.Reviews;
using AsmtAI.Domain.Reviews;
using AsmtAI.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Tests;

public sealed class ReviewDraftRepositoryTests
{
    [Fact]
    public async Task StoresDraftAcrossScopesAndReturnsNullForUnknownId()
    {
        using var provider = new ServiceCollection().AddAssessmentPersistence().BuildServiceProvider();
        Guid id;
        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IReviewDraftRepository>();
            id = await repository.AddAsync(new ReviewDraft("Draft"), CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IReviewDraftRepository>();
            Assert.Equal(new ReviewDraft("Draft"), await repository.FindAsync(id, CancellationToken.None));
            Assert.Null(await repository.FindAsync(Guid.NewGuid(), CancellationToken.None));
        }
    }

    [Fact]
    public async Task KeepsDatabasesIsolatedBetweenApplicationContainers()
    {
        using var first = new ServiceCollection().AddAssessmentPersistence().BuildServiceProvider();
        using var second = new ServiceCollection().AddAssessmentPersistence().BuildServiceProvider();
        using var firstScope = first.CreateScope();
        using var secondScope = second.CreateScope();
        var firstRepository = firstScope.ServiceProvider.GetRequiredService<IReviewDraftRepository>();
        var secondRepository = secondScope.ServiceProvider.GetRequiredService<IReviewDraftRepository>();

        var id = await firstRepository.AddAsync(new ReviewDraft("Private draft"), CancellationToken.None);
        Assert.Null(await secondRepository.FindAsync(id, CancellationToken.None));
    }
}