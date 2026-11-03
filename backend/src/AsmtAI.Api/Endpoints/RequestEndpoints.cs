using System.Net;
using AsmtAI.Application.Requests;
using AsmtAI.Domain.Requests;
using AsmtAI.Domain.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AsmtAI.Api.Endpoints;

public static class RequestEndpoints
{
    private const int MaxPresentationBytes = 10 * 1024 * 1024;

    public static void MapRequestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/requests").WithTags("Local demo requests");
        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";
            if (http.Connection.RemoteIpAddress is not { } address ||
                !(IPAddress.IsLoopback(address) || (address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()))))
                return TypedResults.NotFound();

            var role = http.Request.Headers["X-Demo-Role"].ToString() switch
            {
                "applicant" => AssessmentRole.Applicant,
                "expert" => AssessmentRole.Expert,
                _ => (AssessmentRole?)null
            };
            if (role is null) return TypedResults.BadRequest("A local demo role is required.");
            http.Items[typeof(AssessmentRole)] = role.Value;
            return await next(context);
        });

        group.MapGet("/", async (IAssessmentRequestRepository repository, CancellationToken cancellationToken) =>
            TypedResults.Ok((await repository.ListAsync(cancellationToken)).Select(ToSummary)));

        group.MapPost("/", CreateRequest).WithMetadata(new RequestSizeLimitAttribute(MaxPresentationBytes + 65536));
        group.MapGet("/{id:guid}", GetRequest);
        group.MapGet("/{id:guid}/presentation", GetPresentation);
        group.MapPut("/{id:guid}/review", SaveReview);
    }

    private static async Task<Results<Created<AssessmentRequestSummary>, BadRequest<string>, StatusCodeHttpResult>> CreateRequest(
        HttpContext http, IAssessmentRequestRepository repository, CancellationToken cancellationToken)
    {
        if (GetRole(http) != AssessmentRole.Applicant) return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        if (!http.Request.HasFormContentType) return TypedResults.BadRequest("A presentation and request details are required.");

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var candidateName = form["candidateName"].ToString().Trim();
        var targetLevel = form["targetLevel"].ToString();
        var file = form.Files.GetFile("presentation");
        if (candidateName.Length is 0 or > 100 || targetLevel is not ("A2" or "A3" or "A4"))
            return TypedResults.BadRequest("Enter a candidate name and select an A2, A3, or A4 level.");
        if (file is null || file.Length is 0 or > MaxPresentationBytes)
            return TypedResults.BadRequest("Choose a PowerPoint presentation up to 10 MB.");

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(fileName);
        if (fileName.Length > 200 || !(extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)))
            return TypedResults.BadRequest("The presentation must be a .ppt or .pptx file.");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var request = new AssessmentRequest(candidateName, targetLevel, fileName, stream.ToArray());
        await repository.AddAsync(request, cancellationToken);
        return TypedResults.Created($"/api/requests/{request.Id}", ToSummary(request));
    }

    private static async Task<Results<Ok<AssessmentRequestDetail>, NotFound>> GetRequest(
        Guid id, HttpContext http, IAssessmentRequestRepository repository, CancellationToken cancellationToken)
    {
        var request = await repository.FindAsync(id, cancellationToken);
        if (request is null) return TypedResults.NotFound();
        var expert = GetRole(http) == AssessmentRole.Expert;
        return TypedResults.Ok(new AssessmentRequestDetail(ToSummary(request), expert ? request.Questions : null,
            expert ? request.Transcript : null, expert ? request.Feedback : null));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetPresentation(
        Guid id, HttpContext http, IAssessmentRequestRepository repository, CancellationToken cancellationToken)
    {
        var request = await repository.FindAsync(id, cancellationToken);
        if (request is null) return TypedResults.NotFound();
        http.Response.Headers.XContentTypeOptions = "nosniff";
        return TypedResults.File(request.Presentation, "application/octet-stream", request.PresentationName);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>, StatusCodeHttpResult>> SaveReview(
        Guid id, SaveAssessmentReview input, HttpContext http, IAssessmentRequestRepository repository, CancellationToken cancellationToken)
    {
        if (GetRole(http) != AssessmentRole.Expert) return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        var questions = input.Questions ?? string.Empty;
        var transcript = input.Transcript ?? string.Empty;
        var feedback = input.Feedback ?? string.Empty;
        if (questions.Length > 20000 || transcript.Length > 200000 || feedback.Length > 20000)
            return TypedResults.BadRequest("Review content exceeds the permitted length.");
        if (input.Reviewed && string.IsNullOrWhiteSpace(feedback))
            return TypedResults.BadRequest("Feedback is required to mark a request reviewed.");
        var request = await repository.FindAsync(id, cancellationToken);
        if (request is null) return TypedResults.NotFound();
        request.SaveReview(questions, transcript, feedback, input.Reviewed);
        await repository.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static AssessmentRole GetRole(HttpContext http) => (AssessmentRole)http.Items[typeof(AssessmentRole)]!;
    private static AssessmentRequestSummary ToSummary(AssessmentRequest request) =>
        new(request.Id, request.CandidateName, request.TargetLevel, request.PresentationName, request.CreatedAt, request.Reviewed);
}

/// <summary>Summary of a local assessment request.</summary>
public sealed record AssessmentRequestSummary(Guid Id, string CandidateName, string TargetLevel, string PresentationName, DateTimeOffset CreatedAt, bool Reviewed);

/// <summary>Request details and expert-only review content.</summary>
public sealed record AssessmentRequestDetail(AssessmentRequestSummary Request, string? Questions, string? Transcript, string? Feedback);

/// <summary>Expert-authored questions, transcript, feedback, and review status.</summary>
public sealed record SaveAssessmentReview(string? Questions, string? Transcript, string? Feedback, bool Reviewed);