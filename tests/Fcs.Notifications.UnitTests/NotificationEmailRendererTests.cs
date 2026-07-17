using Fcs.Notifications.Application.Email;
using Fcs.Notifications.Application.Events;
using Fcs.Notifications.Application.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Fcs.Notifications.UnitTests;

public sealed class NotificationTemplateResolverTests
{
    private readonly NotificationTemplateResolver _sut = new(Options.Create(new ResendTemplatesSettings
    {
        DonorWelcomeTemplateId = "tmpl_welcome",
        DonationCreatedTemplateId = "tmpl_created",
        DonationProcessedTemplateId = "tmpl_processed"
    }));

    [Theory]
    [InlineData(NotificationTypes.DonorWelcome, "tmpl_welcome")]
    [InlineData(NotificationTypes.DonationCreated, "tmpl_created")]
    [InlineData(NotificationTypes.DonationProcessed, "tmpl_processed")]
    public void Resolve_KnownType_ReturnsConfiguredTemplate(string type, string templateId)
    {
        var result = _sut.Resolve(type, type == NotificationTypes.DonorWelcome ? null : Guid.NewGuid(), type == NotificationTypes.DonorWelcome ? null : 10m);

        result.TemplateId.Should().Be(templateId);
        if (type == NotificationTypes.DonorWelcome)
        {
            result.Variables.Should().BeEmpty();
        }
        else
        {
            result.Variables.Should().Contain(new KeyValuePair<string, object>("donation_id", result.Variables["donation_id"]));
            result.Variables["amount"].Should().Be("10.00");
        }
    }

    [Fact]
    public void Resolve_UnknownType_ThrowsPermanentError()
    {
        var action = () => _sut.Resolve("Unknown", null, null);

        action.Should().Throw<PermanentNotificationException>();
    }

    [Fact]
    public void Resolve_DonationWithoutRequiredFields_ThrowsPermanentError()
    {
        var action = () => _sut.Resolve(NotificationTypes.DonationCreated, null, 10m);

        action.Should().Throw<PermanentNotificationException>();
    }
}
