using AsmtAI.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace AsmtAI.Infrastructure.Persistence;

public sealed class AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : DbContext(options)
{
    internal DbSet<ReviewDraftEntry> ReviewDrafts => Set<ReviewDraftEntry>();
    internal DbSet<AssessmentRequest> Requests => Set<AssessmentRequest>();
}

internal sealed class ReviewDraftEntry
{
    public Guid Id { get; set; }
    public required string Content { get; set; }
}