using System.Text.RegularExpressions;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Nocturne.API.OpenApi;

/// <summary>
/// Moves the XML <c>&lt;summary&gt;</c> text (which NSwag maps to <c>summary</c>) into
/// <c>description</c>, then replaces <c>summary</c> with a human-readable title derived
/// from the action method name (e.g. <c>CreateEntries</c> → "Create entries").
/// </summary>
public sealed partial class SummaryToDescriptionOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var op = context.OperationDescription.Operation;

        // Move current summary → description (only if description is empty).
        if (!string.IsNullOrWhiteSpace(op.Summary) && string.IsNullOrWhiteSpace(op.Description))
            op.Description = op.Summary;

        // Derive a readable title from the method name.
        var methodName = context.MethodInfo.Name;

        // Strip common suffixes: "Async"
        if (methodName.EndsWith("Async", StringComparison.Ordinal))
            methodName = methodName[..^"Async".Length];

        // "CreateEntries" → "Create entries"
        var words = PascalCaseBoundary().Replace(methodName, " ");
        op.Summary = char.ToUpper(words[0]) + words[1..].ToLowerInvariant();

        return true;
    }

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex PascalCaseBoundary();
}
