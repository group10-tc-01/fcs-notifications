using System.Net;
using Fcs.Notifications.Application.Email;
using FluentAssertions;
using Moq;
using Resend;
using Xunit;

namespace Fcs.Notifications.UnitTests;

public sealed class ResendNotificationEmailSenderTests
{
    [Fact]
    public async Task SendAsync_UsesHostedTemplateVariablesRecipientAndIdempotencyKey()
    {
        var resend = new Mock<IResend>();
        var sut = new ResendNotificationEmailSender(resend.Object);
        var template = new NotificationTemplate("tmpl_donation_created", new Dictionary<string, object>
        {
            ["donation_id"] = "donation-123",
            ["amount"] = "42.50"
        });

        await sut.SendAsync("doador@teste.local", template, "notification/event-123", CancellationToken.None);

        resend.Verify(client => client.EmailSendAsync(
            "notification/event-123",
            It.Is<EmailMessage>(message =>
                message.To.Single().Email == "doador@teste.local" &&
                message.Template!.TemplateId == "tmpl_donation_created" &&
                object.Equals(message.Template!.Variables!["donation_id"], "donation-123") &&
                object.Equals(message.Template!.Variables!["amount"], "42.50")),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendAsync_PermanentResendFailure_ThrowsPermanentNotificationException()
    {
        var resend = new Mock<IResend>();
        resend.Setup(client => client.EmailSendAsync(It.IsAny<string>(), It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResendException(HttpStatusCode.BadRequest, ErrorType.ValidationError, "Invalid template.", null));
        var sut = new ResendNotificationEmailSender(resend.Object);

        var action = () => sut.SendAsync("doador@teste.local", new NotificationTemplate("tmpl_invalid", []), "notification/event-123", CancellationToken.None);

        await action.Should().ThrowAsync<PermanentNotificationException>();
    }

    [Fact]
    public async Task SendAsync_TransientResendFailure_IsPropagatedForRetry()
    {
        var resend = new Mock<IResend>();
        var transientException = new ResendException(HttpStatusCode.ServiceUnavailable, ErrorType.InternalServerError, "Resend unavailable.", null);
        resend.Setup(client => client.EmailSendAsync(It.IsAny<string>(), It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(transientException);
        var sut = new ResendNotificationEmailSender(resend.Object);

        var action = () => sut.SendAsync("doador@teste.local", new NotificationTemplate("tmpl_retry", []), "notification/event-123", CancellationToken.None);

        var assertion = await action.Should().ThrowAsync<ResendException>();

        assertion.Which.Should().BeSameAs(transientException);
    }
}
