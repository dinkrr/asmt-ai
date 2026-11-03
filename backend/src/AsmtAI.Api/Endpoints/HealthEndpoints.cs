namespace AsmtAI.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", (CancellationToken cancellationToken) =>
                TypedResults.Ok(new HealthResponse("ok")))
            .WithName("GetHealth")
            .WithTags("Health");
    }
}

/// <summary>Reports that the API process is responding.</summary>
public sealed record HealthResponse(string Status);