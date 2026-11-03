using System.Net;
using System.Net.Http.Json;
using AsmtAI.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AsmtAI.Api.Tests;

public sealed class RequestEndpointsTests
{
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
        Assert.Equal(new byte[] { 1, 2, 3 }, await expert.GetByteArrayAsync($"{url}/presentation"));
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
        Assert.Equal(HttpStatusCode.BadRequest, (await applicant.PostAsync("/api/requests/", invalidForm)).StatusCode);

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
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/requests/")).StatusCode);

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
    }

    private static MultipartFormDataContent CreateForm()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Demo applicant"), "candidateName");
        form.Add(new StringContent("A3"), "targetLevel");
        form.Add(new ByteArrayContent([1, 2, 3]), "presentation", "slides.pptx");
        return form;
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment, IPAddress address) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ClientAddressFilter(address)));
        });

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