namespace Fcs.Notifications.Application.Email;

public interface INotificationEmailSender
{
    Task SendAsync(string recipientEmail, NotificationTemplate template, string idempotencyKey, CancellationToken cancellationToken);
}

public sealed class PermanentNotificationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
