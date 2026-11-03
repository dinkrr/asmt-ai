namespace AsmtAI.Domain.Requests;

public sealed class AssessmentRequest
{
    private AssessmentRequest() { }

    public AssessmentRequest(string candidateName, string targetLevel, string presentationName, byte[] presentation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLevel);
        ArgumentException.ThrowIfNullOrWhiteSpace(presentationName);
        ArgumentNullException.ThrowIfNull(presentation);
        Id = Guid.NewGuid();
        CandidateName = candidateName.Trim();
        TargetLevel = targetLevel;
        PresentationName = presentationName;
        Presentation = presentation;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string CandidateName { get; private set; } = string.Empty;
    public string TargetLevel { get; private set; } = string.Empty;
    public string PresentationName { get; private set; } = string.Empty;
    public byte[] Presentation { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public string Questions { get; private set; } = string.Empty;
    public string Transcript { get; private set; } = string.Empty;
    public string Feedback { get; private set; } = string.Empty;
    public bool Reviewed { get; private set; }

    public void SaveReview(string questions, string transcript, string feedback, bool reviewed)
    {
        Questions = questions;
        Transcript = transcript;
        Feedback = feedback;
        Reviewed = reviewed;
    }
}