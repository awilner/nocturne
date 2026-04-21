namespace Nocturne.Core.Contracts.Alerts;

/// <summary>
/// Service for acknowledging active alert instances, silencing further escalation
/// until a new excursion begins.
/// </summary>
/// <seealso cref="IAlertOrchestrator"/>
/// <seealso cref="IEscalationAdvancer"/>
public interface IAlertAcknowledgementService
{
    /// <summary>
    /// Acknowledges all active alert instances for the specified tenant, halting
    /// escalation delivery for those instances.
    /// </summary>
    /// <param name="tenantId">The tenant whose alerts should be acknowledged.</param>
    /// <param name="acknowledgedBy">Identifier of the user or system performing the acknowledgement.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when all active instances have been acknowledged.</returns>
    Task AcknowledgeAllAsync(Guid tenantId, string acknowledgedBy, CancellationToken ct);
}
