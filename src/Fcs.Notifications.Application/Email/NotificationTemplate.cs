using Fcs.Notifications.Application.Events;
using Fcs.Notifications.Application.Settings;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Fcs.Notifications.Application.Email;

public sealed record NotificationTemplate(string TemplateId, Dictionary<string, object> Variables);

public interface INotificationTemplateResolver
{
    NotificationTemplate Resolve(string type, Guid? donationId, decimal? amount);
}

public sealed class NotificationTemplateResolver : INotificationTemplateResolver
{
    private readonly ResendTemplatesSettings _settings;

    public NotificationTemplateResolver(IOptions<ResendTemplatesSettings> settings) => _settings = settings.Value;

    public NotificationTemplate Resolve(string type, Guid? donationId, decimal? amount) => type switch
    {
        NotificationTypes.DonorWelcome => new(_settings.DonorWelcomeTemplateId, new Dictionary<string, object>()),
        NotificationTypes.DonationCreated => DonationTemplate(_settings.DonationCreatedTemplateId, donationId, amount),
        NotificationTypes.DonationProcessed => DonationTemplate(_settings.DonationProcessedTemplateId, donationId, amount),
        _ => throw new PermanentNotificationException($"Unsupported notification type '{type}'.")
    };

    private static NotificationTemplate DonationTemplate(string templateId, Guid? donationId, decimal? amount)
    {
        if (donationId is null || amount is null)
        {
            throw new PermanentNotificationException("Donation notifications require donationId and amount.");
        }

        return new NotificationTemplate(templateId, new Dictionary<string, object>
        {
            ["donation_id"] = donationId.Value.ToString(),
            ["amount"] = amount.Value.ToString("F2", CultureInfo.InvariantCulture)
        });
    }
}
