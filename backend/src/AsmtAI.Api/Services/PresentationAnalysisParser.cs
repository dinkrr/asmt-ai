using System.Text.Json;
using AsmtAI.Api.Contracts;

namespace AsmtAI.Api.Services;

public sealed class PresentationAnalysisParser
{
    private static readonly string[] RequiredCategories =
    [
        "Development Experience",
        "Architecture on Practice",
        "Engineering Excellence",
        "Leadership"
    ];

    public PresentationAnalysis Parse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new FormatException("The AI response was empty.");
        if (response.Length > 32000)
            throw new FormatException("The AI response exceeded the permitted length.");

        PresentationAnalysis? analysis;
        try
        {
            analysis = JsonSerializer.Deserialize<PresentationAnalysis>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException exception)
        {
            throw new FormatException("The AI response was not valid review JSON.", exception);
        }

        if (analysis is null || analysis.Categories is null || analysis.Gaps is null || analysis.CandidateFeedback is null ||
            analysis.Categories.Count != RequiredCategories.Length ||
            !RequiredCategories.All(name => analysis.Categories.Count(category => category?.Name == name) == 1) ||
            analysis.Categories.Any(category => category is null || string.IsNullOrWhiteSpace(category.Notes) || category.Notes.Length > 1000 || category.Status is not ("Pass" or "Thin" or "Insufficient")) ||
            analysis.Gaps.Count > 50 || analysis.Gaps.Any(gap => gap is null || !RequiredCategories.Contains(gap.Category) || string.IsNullOrWhiteSpace(gap.Skill) || gap.Skill.Length > 200 || string.IsNullOrWhiteSpace(gap.Reason) || gap.Reason.Length > 1000) ||
            analysis.CandidateFeedback.Count > 20 || analysis.CandidateFeedback.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 1000) ||
            string.IsNullOrWhiteSpace(analysis.DecisionReason) || analysis.DecisionReason.Length > 1500 ||
            analysis.Decision is not ("Approve" or "SendBack"))
            throw new FormatException("The AI response did not match the presentation review structure.");

        var hasInsufficientCategory = analysis.Categories.Any(category => category.Status == "Insufficient");
        if ((analysis.Decision == "SendBack") != hasInsufficientCategory ||
            analysis.Decision == "Approve" && analysis.CandidateFeedback.Count > 0)
            throw new FormatException("The AI response contained an inconsistent recommendation.");

        return analysis;
    }
}