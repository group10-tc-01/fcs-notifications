using Fcs.Notifications.Application.Email;
using Fcs.Notifications.Application.Events;
using Fcs.Notifications.Application.Services;
using Fcs.Notifications.Application.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fcs.Notifications.UnitTests;

public sealed class NotificationProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_ValidDonation_SendsWithStableIdempotencyKey()
    {
        var sender = new FakeEmailSender();
        var sut = CreateSut(sender);
        var eventId = Guid.NewGuid();

        await sut.ProcessAsync(new EmailNotificationRequestedEvent(eventId, NotificationTypes.DonationCreated, "doador@teste.local", Guid.NewGuid(), 42m, DateTime.UtcNow), CancellationToken.None);

        sender.Recipient.Should().Be("doador@teste.local");
        sender.IdempotencyKey.Should().Be($"notification/{eventId}");
        sender.Template!.TemplateId.Should().Be("tmpl_created");
    }

    [Fact]
    public async Task ProcessAsync_DonationWithoutAmount_ThrowsPermanentError()
    {
        var sut = CreateSut(new FakeEmailSender());

        var action = () => sut.ProcessAsync(new EmailNotificationRequestedEvent(Guid.NewGuid(), NotificationTypes.DonationProcessed, "doador@teste.local", Guid.NewGuid(), null, DateTime.UtcNow), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentNotificationException>();
    }

    [Fact]
    public async Task ProcessAsync_UnknownType_ThrowsPermanentError()
    {
        var sut = CreateSut(new FakeEmailSender());

        var action = () => sut.ProcessAsync(new EmailNotificationRequestedEvent(Guid.NewGuid(), "Unknown", "doador@teste.local", null, null, DateTime.UtcNow), CancellationToken.None);

        await action.Should().ThrowAsync<PermanentNotificationException>();
    }

    private static NotificationProcessingService CreateSut(FakeEmailSender sender) =>
        new(new NotificationTemplateResolver(Options.Create(new ResendTemplatesSettings
        {
            DonorWelcomeTemplateId = "tmpl_welcome",
            DonationCreatedTemplateId = "tmpl_created",
            DonationProcessedTemplateId = "tmpl_processed"
        })), sender, NullLogger<NotificationProcessingService>.Instance);

    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public string? Recipient { get; private set; }
        public NotificationTemplate? Template { get; private set; }
        public string? IdempotencyKey { get; private set; }

        public Task SendAsync(string recipientEmail, NotificationTemplate template, string idempotencyKey, CancellationToken cancellationToken)
        {
            Recipient = recipientEmail;
            Template = template;
            IdempotencyKey = idempotencyKey;
            return Task.CompletedTask;
        }
    }
}
