namespace Fcs.Notifications.Application.Events;

public sealed record EmailNotificationRequestedEvent(
    Guid EventId,
    string Type,
    string RecipientEmail,
    Guid? DonationId,
    decimal? Amount,
    DateTime OccurredAt);

public static class NotificationTypes
{
    public const string DonorWelcome = "DonorWelcome";
    public const string DonationCreated = "DonationCreated";
    public const string DonationProcessed = "DonationProcessed";

    public static bool IsKnown(string type) => type is DonorWelcome or DonationCreated or DonationProcessed;
}
