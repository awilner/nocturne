using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Nocturne.API.Infrastructure;

/// <summary>
/// Removes controllers in the .DevOnly namespace from non-development environments.
/// </summary>
internal class RemoveDevOnlyControllersFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        if (typeInfo.Namespace?.Contains(".DevOnly", StringComparison.Ordinal) == true)
            return false;
        return base.IsController(typeInfo);
    }
}
