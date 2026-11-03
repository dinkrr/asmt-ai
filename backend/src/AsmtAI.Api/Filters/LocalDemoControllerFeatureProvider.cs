using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AsmtAI.Api.Filters;

public sealed class LocalDemoControllerFeatureProvider(IHostEnvironment environment) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        if (environment.IsDevelopment()) return;
        foreach (var controller in feature.Controllers.Where(controller => controller.IsDefined(typeof(LocalDemoOnlyAttribute), inherit: true)).ToArray())
            feature.Controllers.Remove(controller);
    }
}