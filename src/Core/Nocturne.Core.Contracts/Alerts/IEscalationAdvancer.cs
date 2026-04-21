using Nocturne.Core.Models;

namespace Nocturne.Core.Contracts.Alerts;

/// <summary>
/// Advances an active alert instance to its next escalation step, dispatching
/// the notification for that step via <see cref="IAlertDeliveryService"/>.
/// Called by a background timer that polls for due escalations.
/// </summary>
/// <seealso cref="IAlertDeliveryService"/>
/// <seealso cref="IAlertRepository"/>
public interface IEscalationAdvancer
{
    /// <summary>
    /// Advances the given alert instance to the next escalation step and dispatches
    /// the corresponding notification.
    /// </summary>
    /// <param name="instance">The <see cref="AlertInstanceSnapshot"/> to advance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the instance has been advanced and the delivery dispatched.</returns>
    Task AdvanceAsync(AlertInstanceSnapshot instance, CancellationToken ct);
}
