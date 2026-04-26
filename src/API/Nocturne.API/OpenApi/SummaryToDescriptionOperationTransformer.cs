using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Nocturne.API.OpenApi;

/// <summary>
/// Microsoft OpenAPI transformer that mirrors <see cref="SummaryToDescriptionOperationProcessor"/>
/// for runtime Scalar docs. Moves the XML summary into description and derives a readable
/// title from the action method name.
/// </summary>
public sealed partial class SummaryToDescriptionOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Move current summary → description (only if description is empty).
        if (!string.IsNullOrWhiteSpace(operation.Summary) && string.IsNullOrWhiteSpace(operation.Description))
            operation.Description = operation.Summary;

        // Derive a readable title from the action method name.
        var methodName = (context.Description.ActionDescriptor as ControllerActionDescriptor)
            ?.MethodInfo.Name;

        if (methodName is null)
            return Task.CompletedTask;

        if (methodName.EndsWith("Async", StringComparison.Ordinal))
            methodName = methodName[..^"Async".Length];

        var words = PascalCaseBoundary().Replace(methodName, " ");
        operation.Summary = char.ToUpper(words[0]) + words[1..].ToLowerInvariant();

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex PascalCaseBoundary();
}
