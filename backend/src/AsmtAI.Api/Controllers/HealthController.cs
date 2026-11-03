using Microsoft.AspNetCore.Mvc;

namespace AsmtAI.Api.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet(Name = "GetHealth")]
    [EndpointSummary("Check whether the API process is responding")]
    public ActionResult<HealthResponse> Get(CancellationToken cancellationToken) => Ok(new HealthResponse("ok"));
}

/// <summary>Reports that the API process is responding.</summary>
public sealed record HealthResponse(string Status);