using FluentValidation;
using Nocturne.API.Controllers.V4.Monitoring;

namespace Nocturne.API.Validators.Alerts;

/// <summary>
/// Validates <see cref="UpdateAlertRuleRequest"/> for the V4 alert rule update endpoint.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Name is required and capped at 200 characters.</description></item>
/// <item><description>Description, when provided, is capped at 2,000 characters.</description></item>
/// <item><description>ConditionType must be a valid enum value.</description></item>
/// <item><description>HysteresisMinutes and ConfirmationReadings must be non-negative.</description></item>
/// <item><description>Severity, when provided, must be a valid enum value.</description></item>
/// <item><description>At least one schedule must be marked as default if schedules are provided.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="UpdateAlertRuleRequest"/>
/// <seealso cref="AlertRulesController"/>
public class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAlertRuleRequestValidator"/> class
    /// and configures all validation rules for alert rule updates.
    /// </summary>
    public UpdateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.ConditionType).IsInEnum();
        RuleFor(x => x.HysteresisMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConfirmationReadings).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Severity).IsInEnum().When(x => x.Severity is not null);
        RuleFor(x => x.Schedules).Must(s => s == null || s.Any(sch => sch.IsDefault))
            .WithMessage("At least one schedule must be marked as default");
    }
}
