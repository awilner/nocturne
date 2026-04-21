using Nocturne.Core.Models;

namespace Nocturne.Core.Contracts.Alerts;

/// <summary>
/// Manages the lifecycle of individual alert deliveries: dispatching notifications
/// through configured channels and recording their outcome.
/// </summary>
/// <seealso cref="IAlertOrchestrator"/>
/// <seealso cref="IEscalationAdvancer"/>
/// <seealso cref="AlertPayload"/>
public interface IAlertDeliveryService
{
    /// <summary>
    /// Dispatches a notification for a specific escalation step of an alert instance.
    /// </summary>
    /// <param name="alertInstanceId">The alert instance that triggered the delivery.</param>
    /// <param name="stepOrder">The escalation step order (0-based) determining which channel to use.</param>
    /// <param name="payload">The <see cref="AlertPayload"/> containing the notification content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the delivery has been dispatched.</returns>
    Task DispatchAsync(Guid alertInstanceId, int stepOrder, AlertPayload payload, CancellationToken ct);

    /// <summary>
    /// Records a successful delivery with optional platform-specific identifiers for
    /// future reference (e.g., editing or threading follow-up messages).
    /// </summary>
    /// <param name="deliveryId">The delivery record to update.</param>
    /// <param name="platformMessageId">Platform-specific message identifier, if available.</param>
    /// <param name="platformThreadId">Platform-specific thread identifier, if available.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the delivery record has been updated.</returns>
    Task MarkDeliveredAsync(Guid deliveryId, string? platformMessageId, string? platformThreadId, CancellationToken ct);

    /// <summary>
    /// Records a failed delivery attempt with the error details.
    /// </summary>
    /// <param name="deliveryId">The delivery record to update.</param>
    /// <param name="error">A description of the failure.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the delivery record has been updated.</returns>
    Task MarkFailedAsync(Guid deliveryId, string error, CancellationToken ct);
}
