using System.Net;
using System.Net.Http.Json;
using AsmtAI.Api.Contracts;
using AsmtAI.Application.Reviews;
using AsmtAI.Domain.Reviews;
using AsmtAI.Api.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace AsmtAI.Api.Tests;

public sealed class RequestEndpointsTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task HealthEndpointIsAvailableInAllEnvironments(string environment)
    {
        using var factory = CreateFactory(environment, IPAddress.Loopback);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ApplicantAndExpertCanOnlyUseTheirDemoActions()
    {
        using var factory = CreateFactory("Development", IPAddress.IPv6Loopback);
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");

        using var form = CreateForm();
        var created = await applicant.PostAsync("/api/requests/", form);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("no-store", created.Headers.CacheControl?.ToString());
        var summary = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(summary);
        var url = $"/api/requests/{summary.Id}";
        Assert.Equal(url, created.Headers.Location?.OriginalString);

        using var expertForm = CreateForm();
        Assert.Equal(HttpStatusCode.Forbidden, (await expert.PostAsync("/api/requests/", expertForm)).StatusCode);
        var review = new SaveAssessmentReview("Question", "Transcript", "Feedback", true);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.PutAsJsonAsync($"{url}/review", review)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await expert.PutAsJsonAsync($"{url}/review", review)).StatusCode);

        var expertDetail = await expert.GetFromJsonAsync<AssessmentRequestDetail>(url);
        var applicantDetail = await applicant.GetFromJsonAsync<AssessmentRequestDetail>(url);
        Assert.Equal("Transcript", expertDetail?.Transcript);
        Assert.Equal("Feedback", expertDetail?.Feedback);
        Assert.Null(applicantDetail?.Transcript);
        Assert.Null(applicantDetail?.Feedback);
        var presentation = await expert.GetAsync($"{url}/presentation");
        Assert.Equal("nosniff", presentation.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(new byte[] { 1, 2, 3 }, await presentation.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ValidatesPresentationAndReviewCompletion()
    {
        using var factory = CreateFactory("Development", IPAddress.Loopback);
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");

        using var invalidForm = new MultipartFormDataContent();
        invalidForm.Add(new StringContent("Demo applicant"), "candidateName");
        invalidForm.Add(new StringContent("A3"), "targetLevel");
        invalidForm.Add(new ByteArrayContent([1]), "presentation", "notes.txt");
        var invalidResponse = await applicant.PostAsync("/api/requests/", invalidForm);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("application/json", invalidResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("The presentation must be a .ppt or .pptx file.", await invalidResponse.Content.ReadFromJsonAsync<string>());

        using var oversizedForm = new MultipartFormDataContent();
        oversizedForm.Add(new StringContent("Demo applicant"), "candidateName");
        oversizedForm.Add(new StringContent("A3"), "targetLevel");
        oversizedForm.Add(new ByteArrayContent(new byte[10 * 1024 * 1024 + 1]), "presentation", "slides.pptx");
        var oversizedResponse = await applicant.PostAsync("/api/requests/", oversizedForm);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedResponse.StatusCode);
        Assert.Equal("Choose a PowerPoint presentation up to 10 MB.", await oversizedResponse.Content.ReadFromJsonAsync<string>());

        using var validForm = CreateForm();
        var created = await applicant.PostAsync("/api/requests/", validForm);
        var summary = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(summary);
        var review = new SaveAssessmentReview("Question", "Transcript", " ", true);
        Assert.Equal(HttpStatusCode.BadRequest, (await expert.PutAsJsonAsync($"/api/requests/{summary.Id}/review", review)).StatusCode);
    }

    [Fact]
    public async Task DemoEndpointsRejectMissingRoleAndNonLoopbackClients()
    {
        using var factory = CreateFactory("Development", IPAddress.Loopback);
        using var client = factory.CreateClient();
        var noRole = await client.GetAsync("/api/requests/");
        Assert.Equal(HttpStatusCode.BadRequest, noRole.StatusCode);
        Assert.Equal("no-store", noRole.Headers.CacheControl?.ToString());
        Assert.Equal("A local demo role is required.", await noRole.Content.ReadFromJsonAsync<string>());
        using var malformedReview = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync($"/api/requests/{Guid.NewGuid()}/review", malformedReview)).StatusCode);

        using var remoteFactory = CreateFactory("Development", IPAddress.Parse("192.0.2.1"));
        using var remote = remoteFactory.CreateClient();
        remote.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        Assert.Equal(HttpStatusCode.NotFound, (await remote.GetAsync("/api/requests/")).StatusCode);
    }

    [Fact]
    public async Task DemoEndpointsAreUnavailableOutsideDevelopment()
    {
        using var factory = CreateFactory("Production", IPAddress.Loopback);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/requests/")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/requests/")).StatusCode);
    }

    [Fact]
    public async Task DevelopmentOpenApiDocumentsControllerRoutes()
    {
        using var factory = CreateFactory("Development", IPAddress.Loopback);
        using var client = factory.CreateClient();
        var document = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/health", document);
        Assert.Contains("/api/requests", document);
        Assert.Contains("/api/requests/{id}/review", document);
    }

    [Fact]
    public async Task PresentationAnalysisIsExpertOnlyAndReturnsAnUnsavedDraft()
    {
        var assistant = new RecordingAssistant();
        using var factory = CreateFactory("Development", IPAddress.Loopback, assistant);
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        using var form = CreateForm(CreatePresentation());
        var created = await applicant.PostAsync("/api/requests/", form);
        var request = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(request);
        var url = $"/api/requests/{request.Id}/presentation/analysis";

        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.PostAsync(url, null)).StatusCode);
        Assert.Null(assistant.Input);
        var response = await expert.PostAsync(url, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var analysis = await response.Content.ReadFromJsonAsync<PresentationAnalysis>();
        Assert.Equal(4, analysis?.Categories.Count);
        Assert.Equal("Approve", analysis?.Decision);
        Assert.Contains("Synthetic slide text", assistant.Input);
        Assert.Contains("Demo applicant", assistant.Input);
        Assert.Null((await applicant.GetFromJsonAsync<AssessmentRequestDetail>($"/api/requests/{request.Id}"))?.Feedback);
    }

    [Fact]
    public async Task PresentationAnalysisRejectsUnreadableFileBeforeCallingAssistant()
    {
        var assistant = new RecordingAssistant();
        using var factory = CreateFactory("Development", IPAddress.Loopback, assistant);
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        using var form = CreateForm();
        var created = await applicant.PostAsync("/api/requests/", form);
        var request = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(request);
        Assert.Equal(HttpStatusCode.BadRequest, (await expert.PostAsync($"/api/requests/{request.Id}/presentation/analysis", null)).StatusCode);
        Assert.Null(assistant.Input);
    }

    [Fact]
    public async Task PresentationAnalysisReportsProviderFailuresWithoutLeakingDetails()
    {
        using var factory = CreateFactory("Development", IPAddress.Loopback, new FailingAssistant());
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        using var form = CreateForm(CreatePresentation());
        var created = await applicant.PostAsync("/api/requests/", form);
        var request = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(request);
        var response = await expert.PostAsync($"/api/requests/{request.Id}/presentation/analysis", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private provider error", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PresentationAnalysisRejectsMalformedProviderOutput()
    {
        using var factory = CreateFactory("Development", IPAddress.Loopback, new MalformedAssistant());
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        using var form = CreateForm(CreatePresentation());
        var created = await applicant.PostAsync("/api/requests/", form);
        var request = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(request);
        var response = await expert.PostAsync($"/api/requests/{request.Id}/presentation/analysis", null);
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.DoesNotContain("private model content", await response.Content.ReadAsStringAsync());
    }

        [Fact]
        public void PresentationAnalysisParserRejectsIncompleteOrInconsistentReviews()
        {
                var parser = new PresentationAnalysisParser();
                Assert.Throws<FormatException>(() => parser.Parse("# A markdown review, not a JSON object"));
                Assert.Throws<FormatException>(() => parser.Parse("""
                        {
                            "categories": [
                                { "name": "Development Experience", "status": "Insufficient", "notes": "No evidence." },
                                { "name": "Architecture on Practice", "status": "Pass", "notes": "Evidence is present." },
                                { "name": "Engineering Excellence", "status": "Pass", "notes": "Evidence is present." },
                                { "name": "Leadership", "status": "Pass", "notes": "Evidence is present." }
                            ],
                            "gaps": [], "candidateFeedback": [], "decision": "Approve", "decisionReason": "Proceed."
                        }
                        """));
        }

    [LiveCopilotFact]
    public async Task SyntheticPresentationCanBeAnalyzedThroughTheRealCopilotProvider()
    {
        var root = Environment.GetEnvironmentVariable("ASMT_AI_LIVE_AGENT_REPOSITORY");
        using var factory = CreateFactory("Development", IPAddress.Loopback).WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "copilot",
                ["Ai:AgentRepositoryPath"] = root
            })));
        using var applicant = factory.CreateClient();
        using var expert = factory.CreateClient();
        expert.Timeout = TimeSpan.FromMinutes(3);
        applicant.DefaultRequestHeaders.Add("X-Demo-Role", "applicant");
        expert.DefaultRequestHeaders.Add("X-Demo-Role", "expert");
        using var form = CreateForm(CreatePresentation());
        var created = await applicant.PostAsync("/api/requests/", form);
        var request = await created.Content.ReadFromJsonAsync<AssessmentRequestSummary>();
        Assert.NotNull(request);
        var response = await expert.PostAsync($"/api/requests/{request.Id}/presentation/analysis", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, (await response.Content.ReadFromJsonAsync<PresentationAnalysis>())?.Categories.Count);
    }

    private static MultipartFormDataContent CreateForm(byte[]? presentation = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Demo applicant"), "candidateName");
        form.Add(new StringContent("A3"), "targetLevel");
        form.Add(new ByteArrayContent(presentation ?? [1, 2, 3]), "presentation", "slides.pptx");
        return form;
    }

    private static byte[] CreatePresentation()
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
        {
            var presentation = document.AddPresentationPart();
            presentation.Presentation = new Presentation(new SlideIdList());
            var slide = presentation.AddNewPart<SlidePart>();
            slide.Slide = new Slide(new CommonSlideData(new ShapeTree(new Shape(new TextBody(
                new Drawing.BodyProperties(), new Drawing.ListStyle(), new Drawing.Paragraph(new Drawing.Run(new Drawing.Text("Synthetic slide text"))))))));
            presentation.Presentation.SlideIdList!.Append(new SlideId { Id = 256U, RelationshipId = presentation.GetIdOfPart(slide) });
        }
        return stream.ToArray();
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment, IPAddress address, IAssessmentAssistant? assistant = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new ClientAddressFilter(address));
                if (assistant is not null) services.AddSingleton(assistant);
            });
        });

    private sealed class RecordingAssistant : IAssessmentAssistant
    {
        public string? Input { get; private set; }
        public Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken)
        {
            Input = presentation;
                        return Task.FromResult(new ReviewDraft("""
                                {
                                    "categories": [
                                        { "name": "Development Experience", "status": "Pass", "notes": "Relevant examples are present." },
                                        { "name": "Architecture on Practice", "status": "Thin", "notes": "Trade-offs need more depth." },
                                        { "name": "Engineering Excellence", "status": "Pass", "notes": "Testing and delivery are covered." },
                                        { "name": "Leadership", "status": "Pass", "notes": "Mentoring is described." }
                                    ],
                                    "gaps": [ { "category": "Development Experience", "skill": "Async", "reason": "No example is given." } ],
                                    "candidateFeedback": [],
                                    "decision": "Approve",
                                    "decisionReason": "All major categories are represented."
                                }
                                """));
        }
    }

    private sealed class FailingAssistant : IAssessmentAssistant
    {
        public Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("private provider error");
    }

    private sealed class MalformedAssistant : IAssessmentAssistant
    {
        public Task<ReviewDraft> GenerateReviewAsync(string presentation, CancellationToken cancellationToken) =>
            Task.FromResult(new ReviewDraft("private model content"));
    }

    public sealed class LiveCopilotFactAttribute : FactAttribute
    {
        public LiveCopilotFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASMT_AI_LIVE_AGENT_REPOSITORY")))
                Skip = "Set ASMT_AI_LIVE_AGENT_REPOSITORY to enable the live Copilot test.";
        }
    }

    private sealed class ClientAddressFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                await nextMiddleware(context);
            });
            next(app);
        };
    }
}