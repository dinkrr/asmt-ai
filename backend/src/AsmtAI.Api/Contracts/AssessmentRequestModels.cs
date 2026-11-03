namespace AsmtAI.Api.Contracts;

/// <summary>Summary of a local assessment request.</summary>
public sealed record AssessmentRequestSummary(Guid Id, string CandidateName, string TargetLevel, string PresentationName, DateTimeOffset CreatedAt, bool Reviewed);

/// <summary>Request details and expert-only review content.</summary>
public sealed record AssessmentRequestDetail(AssessmentRequestSummary Request, string? Questions, string? Transcript, string? Feedback);

/// <summary>Expert-authored questions, transcript, feedback, and review status.</summary>
public sealed record SaveAssessmentReview(string? Questions, string? Transcript, string? Feedback, bool Reviewed);