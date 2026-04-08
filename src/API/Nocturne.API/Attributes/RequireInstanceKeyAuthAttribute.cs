using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nocturne.Core.Models.Authorization;

namespace Nocturne.API.Attributes;

/// <summary>
/// Restricts an endpoint to requests authenticated via X-Instance-Key only.
/// Regular cookie/token authenticated users will receive 403 Forbidden.
/// Used for server-to-server cross-tenant endpoints that must not be reachable
/// from a normal user session.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class RequireInstanceKeyAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authContext = context.HttpContext.Items["AuthContext"] as AuthContext;
        if (authContext?.AuthType != AuthType.InstanceKey)
        {
            context.Result = new ForbidResult();
        }
    }
}
