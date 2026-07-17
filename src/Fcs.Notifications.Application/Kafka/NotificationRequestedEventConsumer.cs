using Fcs.Notifications.Application.Common.Abstractions;
using Fcs.Notifications.Application.Email;
using Fcs.Notifications.Application.Events;
using Fcs.Notifications.Application.Services;
using Fcs.Notifications.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Fcs.Notifications.Application.Kafka;

[ExcludeFromCodeCoverage]
public sealed class EmailNotificationRequestedEventConsumer : BaseKafkaConsumer<EmailNotificationRequestedEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailNotificationRequestedEventConsumer> _logger;

    public EmailNotificationRequestedEventConsumer(
        ILogger<EmailNotificationRequestedEventConsumer> logger,
        IOptions<KafkaSettings> kafkaSettings,
        IServiceScopeFactory serviceScopeFactory)
        : base(
            logger,
            kafkaSettings.Value.BootstrapServers,
            kafkaSettings.Value.GroupId,
            kafkaSettings.Value.Topics.EmailNotification,
            kafkaSettings.Value.ConsumerTimeoutMs)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(EmailNotificationRequestedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<NotificationProcessingService>();
            await processingService.ProcessAsync(@event, cancellationToken);
        }
        catch (PermanentNotificationException exception)
        {
            _logger.LogWarning(exception, "Discarding invalid or permanent notification failure for event {EventId}.", @event.EventId);
        }
    }
}
