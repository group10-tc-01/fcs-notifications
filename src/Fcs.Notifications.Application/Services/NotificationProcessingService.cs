using Fcs.Notifications.Application.Email;
using Fcs.Notifications.Application.Events;
using Microsoft.Extensions.Logging;

namespace Fcs.Notifications.Application.Services;

public sealed class NotificationProcessingService
{
    private readonly INotificationTemplateResolver _templateResolver;
    private readonly INotificationEmailSender _sender;
    private readonly ILogger<NotificationProcessingService> _logger;

    public NotificationProcessingService(
        INotificationTemplateResolver templateResolver,
        INotificationEmailSender sender,
        ILogger<NotificationProcessingService> logger)
    {
        _templateResolver = templateResolver;
        _sender = sender;
        _logger = logger;
    }

    public async Task ProcessAsync(EmailNotificationRequestedEvent @event, CancellationToken cancellationToken)
    {
        Validate(@event);
        var template = _templateResolver.Resolve(@event.Type, @event.DonationId, @event.Amount);
        await _sender.SendAsync(@event.RecipientEmail, template, $"notification/{@event.EventId}", cancellationToken);
        _logger.LogInformation("Notification {EventId} of type {NotificationType} was accepted by Resend.", @event.EventId, @event.Type);
    }

    private static void Validate(EmailNotificationRequestedEvent @event)
    {
        if (@event.EventId == Guid.Empty || !NotificationTypes.IsKnown(@event.Type) || string.IsNullOrWhiteSpace(@event.RecipientEmail))
        {
            throw new PermanentNotificationException("Notification event is invalid.");
        }

    }
}
