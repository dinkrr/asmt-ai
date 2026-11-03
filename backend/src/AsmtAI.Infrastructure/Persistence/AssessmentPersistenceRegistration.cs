using AsmtAI.Application.Reviews;
using AsmtAI.Application.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Infrastructure.Persistence;

public static class AssessmentPersistenceRegistration
{
    public static IServiceCollection AddAssessmentPersistence(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDatabaseRoot>();
        services.AddDbContext<AssessmentDbContext>((provider, options) =>
            options.UseInMemoryDatabase("Assessment", provider.GetRequiredService<InMemoryDatabaseRoot>()));
        services.AddScoped<IReviewDraftRepository, ReviewDraftRepository>();
        services.AddScoped<IAssessmentRequestRepository, AssessmentRequestRepository>();
        return services;
    }
}