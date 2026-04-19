using FluentValidation;
using Nocturne.API.Models.Requests.V4;

namespace Nocturne.API.Validators.V4;

public class CreateMealRequestValidator : AbstractValidator<CreateMealRequest>
{
    public CreateMealRequestValidator()
    {
        RuleFor(x => x.Timestamp).NotEqual(default(DateTimeOffset)).WithMessage("Timestamp is required");
        RuleFor(x => x.Device).MaximumLength(500).When(x => x.Device is not null);
        RuleFor(x => x.App).MaximumLength(500).When(x => x.App is not null);
        RuleFor(x => x.DataSource).MaximumLength(500).When(x => x.DataSource is not null);
        RuleFor(x => x.Insulin).GreaterThanOrEqualTo(0).WithMessage("Insulin must be >= 0");
        RuleFor(x => x.Carbs).GreaterThanOrEqualTo(0).WithMessage("Carbs must be >= 0");
        RuleFor(x => x.Duration).GreaterThanOrEqualTo(0).When(x => x.Duration is not null)
            .WithMessage("Duration must be >= 0");
        RuleFor(x => x.AbsorptionTime).GreaterThanOrEqualTo(0).When(x => x.AbsorptionTime is not null)
            .WithMessage("AbsorptionTime must be >= 0");
        RuleFor(x => x.SyncIdentifier).MaximumLength(500).When(x => x.SyncIdentifier is not null);
        RuleFor(x => x.InsulinType).MaximumLength(500).When(x => x.InsulinType is not null);
        RuleFor(x => x.DataSource)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.SyncIdentifier))
            .WithMessage("DataSource is required when SyncIdentifier is supplied.");
        RuleFor(x => x.CorrelationId)
            .Must(id => id != Guid.Empty)
            .When(x => x.CorrelationId.HasValue)
            .WithMessage("CorrelationId must be a non-empty GUID when supplied.");
    }
}
