namespace Nocturne.Core.Contracts;

/// <summary>
/// Handles notification-specific actions. Registered per notification type string.
/// </summary>
public interface INotificationActionHandler
{
    /// <summary>
    /// The notification type string this handler is responsible for (e.g., "SuggestedMealMatch")
    /// </summary>
    string NotificationType { get; }

    /// <summary>
    /// Handle an action on a notification of this type.
    /// </summary>
    /// <param name="notificationId">The notification ID being acted on.</param>
    /// <param name="actionId">The specific action to execute (e.g., "accept", "dismiss").</param>
    /// <param name="userId">The user executing the action.</param>
    /// <param name="sourceId">The source entity ID from the notification, if any.</param>
    /// <param name="metadata">Notification-specific metadata, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action was handled successfully, false otherwise.</returns>
    Task<bool> HandleAsync(
        Guid notificationId,
        string actionId,
        string userId,
        string? sourceId,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken = default);
}
