using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Nocturne.API.OpenApi;

/// <summary>
/// Forces NSwag to use the controller name (without "Controller" suffix) as the tag,
/// ignoring any [Tags] attributes. This keeps the TypeScript client codegen granular
/// (one client class per controller) while [Tags] attributes only affect the runtime
/// Microsoft OpenAPI pipeline used by Scalar.
/// </summary>
public sealed class ControllerNameTagOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var controllerName = context.ControllerType.Name;

        if (controllerName.EndsWith("Controller", StringComparison.Ordinal))
            controllerName = controllerName[..^"Controller".Length];

        context.OperationDescription.Operation.Tags.Clear();
        context.OperationDescription.Operation.Tags.Add(controllerName);

        return true;
    }
}
