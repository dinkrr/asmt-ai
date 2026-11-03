using AsmtAI.Api.Contracts;
using AsmtAI.Api.Filters;
using AsmtAI.Api.Services;
using AsmtAI.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace AsmtAI.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Tags("Local demo requests")]
[LocalDemoOnly]
public sealed class RequestsController(IAssessmentRequestService service) : ControllerBase
{
    private const int MaxPresentationBytes = 10 * 1024 * 1024;

    [HttpGet]
    [EndpointSummary("List local demo requests")]
    public async Task<ActionResult<IReadOnlyList<AssessmentRequestSummary>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    [HttpPost]
    [RequestSizeLimit(MaxPresentationBytes + 65536)]
    [EndpointSummary("Submit a local demo request with a presentation")]
    [ProducesResponseType<AssessmentRequestSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssessmentRequestSummary>> Create(CancellationToken cancellationToken)
    {
        if (GetRole() != AssessmentRole.Applicant) return StatusCode(StatusCodes.Status403Forbidden);
        if (!Request.HasFormContentType) return BadRequestMessage("A presentation and request details are required.");

        var form = await Request.ReadFormAsync(cancellationToken);
        var candidateName = form["candidateName"].ToString().Trim();
        var targetLevel = form["targetLevel"].ToString();
        var file = form.Files.GetFile("presentation");
        if (candidateName.Length is 0 or > 100 || targetLevel is not ("A2" or "A3" or "A4"))
            return BadRequestMessage("Enter a candidate name and select an A2, A3, or A4 level.");
        if (file is null || file.Length is 0 or > MaxPresentationBytes)
            return BadRequestMessage("Choose a PowerPoint presentation up to 10 MB.");

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(fileName);
        if (fileName.Length > 200 || !(extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)))
            return BadRequestMessage("The presentation must be a .ppt or .pptx file.");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var request = await service.CreateAsync(candidateName, targetLevel, fileName, stream.ToArray(), cancellationToken);
        return Created($"/api/requests/{request.Id}", request);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a local demo request")]
    [ProducesResponseType<AssessmentRequestDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentRequestDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        var request = await service.GetAsync(id, GetRole() == AssessmentRole.Expert, cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpGet("{id:guid}/presentation")]
    [EndpointSummary("Download a local demo presentation")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPresentation(Guid id, CancellationToken cancellationToken)
    {
        var presentation = await service.GetPresentationAsync(id, cancellationToken);
        if (presentation is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(presentation.Content, "application/octet-stream", presentation.FileName);
    }

    [HttpPut("{id:guid}/review")]
    [EndpointSummary("Save an expert-authored review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveReview(Guid id, SaveAssessmentReview input, CancellationToken cancellationToken)
    {
        if (GetRole() != AssessmentRole.Expert) return StatusCode(StatusCodes.Status403Forbidden);
        var questions = input.Questions ?? string.Empty;
        var transcript = input.Transcript ?? string.Empty;
        var feedback = input.Feedback ?? string.Empty;
        if (questions.Length > 20000 || transcript.Length > 200000 || feedback.Length > 20000)
            return BadRequestMessage("Review content exceeds the permitted length.");
        if (input.Reviewed && string.IsNullOrWhiteSpace(feedback))
            return BadRequestMessage("Feedback is required to mark a request reviewed.");
        return await service.SaveReviewAsync(id, input, cancellationToken) ? NoContent() : NotFound();
    }

    private AssessmentRole GetRole() => (AssessmentRole)HttpContext.Items[typeof(AssessmentRole)]!;
    private static JsonResult BadRequestMessage(string message) => new(message) { StatusCode = StatusCodes.Status400BadRequest };
}