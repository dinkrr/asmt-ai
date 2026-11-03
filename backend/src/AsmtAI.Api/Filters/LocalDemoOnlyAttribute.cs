using System.Net;
using AsmtAI.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AsmtAI.Api.Filters;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LocalDemoOnlyAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            context.Result = new NotFoundResult();
            return;
        }

        http.Response.Headers.CacheControl = "no-store";
        if (http.Connection.RemoteIpAddress is not { } address ||
            !(IPAddress.IsLoopback(address) || (address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()))))
        {
            context.Result = new NotFoundResult();
            return;
        }

        var role = http.Request.Headers["X-Demo-Role"].ToString() switch
        {
            "applicant" => AssessmentRole.Applicant,
            "expert" => AssessmentRole.Expert,
            _ => (AssessmentRole?)null
        };
        if (role is null)
        {
            context.Result = new JsonResult("A local demo role is required.") { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        http.Items[typeof(AssessmentRole)] = role.Value;
        await next();
    }
}