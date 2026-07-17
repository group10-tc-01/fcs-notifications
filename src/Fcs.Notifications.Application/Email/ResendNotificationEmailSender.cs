using Resend;
using System.Net;

namespace Fcs.Notifications.Application.Email;

public sealed class ResendNotificationEmailSender : INotificationEmailSender
{
    private readonly IResend _resend;
    public ResendNotificationEmailSender(IResend resend)
    {
        _resend = resend;
    }

    public async Task SendAsync(string recipientEmail, NotificationTemplate template, string idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessage
            {
                Template = new EmailMessageTemplate
                {
                    TemplateId = template.TemplateId,
                    Variables = template.Variables
                }
            };
            message.To.Add(recipientEmail);

            await _resend.EmailSendAsync(idempotencyKey, message, cancellationToken);
        }
        catch (ResendException exception) when (!exception.IsTransient && !IsRetryableStatusCode(exception.StatusCode))
        {
            throw new PermanentNotificationException("Resend rejected the notification request.", exception);
        }
    }

    private static bool IsRetryableStatusCode(HttpStatusCode? statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        || statusCode is not null && (int)statusCode >= 500;
}
