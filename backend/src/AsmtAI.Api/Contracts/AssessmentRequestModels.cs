namespace AsmtAI.Api.Contracts;

/// <summary>Summary of a local assessment request.</summary>
public sealed record AssessmentRequestSummary(Guid Id, string CandidateName, string TargetLevel, string PresentationName, DateTimeOffset CreatedAt, bool Reviewed);

/// <summary>Request details and expert-only review content.</summary>
public sealed record AssessmentRequestDetail(AssessmentRequestSummary Request, string? Questions, string? Transcript, string? Feedback);

/// <summary>Expert-authored questions, transcript, feedback, and review status.</summary>
public sealed record SaveAssessmentReview(string? Questions, string? Transcript, string? Feedback, bool Reviewed);

/// <summary>AI-generated presentation analysis for expert review.</summary>
public sealed record PresentationAnalysis(
	IReadOnlyList<PresentationCategoryAssessment> Categories,
	IReadOnlyList<PresentationGap> Gaps,
	IReadOnlyList<string> CandidateFeedback,
	string Decision,
	string DecisionReason);

/// <summary>Assessment of one presentation category.</summary>
public sealed record PresentationCategoryAssessment(string Name, string Status, string Notes);

/// <summary>A required skill to probe during the assessment session.</summary>
public sealed record PresentationGap(string Category, string Skill, string Reason);