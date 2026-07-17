using Fcs.Notifications.Application.Email;
using Fcs.Notifications.Application.Kafka;
using Fcs.Notifications.Application.Services;
using Fcs.Notifications.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;
using System.Diagnostics.CodeAnalysis;

namespace Fcs.Notifications.Application.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KafkaSettings>().Bind(configuration.GetRequiredSection(KafkaSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.BootstrapServers) && !string.IsNullOrWhiteSpace(settings.GroupId) && !string.IsNullOrWhiteSpace(settings.Topics.EmailNotification), "Kafka settings are required.")
            .ValidateOnStart();

        services.AddOptions<ResendSettings>().Bind(configuration.GetRequiredSection(ResendSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey), "Resend API key is required.")
            .ValidateOnStart();

        services.AddOptions<ResendTemplatesSettings>().Bind(configuration.GetRequiredSection(ResendTemplatesSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DonorWelcomeTemplateId) && !string.IsNullOrWhiteSpace(settings.DonationCreatedTemplateId) && !string.IsNullOrWhiteSpace(settings.DonationProcessedTemplateId), "Resend template identifiers are required.")
            .ValidateOnStart();

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration.GetRequiredSection(ResendSettings.SectionName)[nameof(ResendSettings.ApiKey)]!;
            options.ThrowExceptions = true;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddSingleton<INotificationTemplateResolver, NotificationTemplateResolver>();
        services.AddTransient<INotificationEmailSender, ResendNotificationEmailSender>();
        services.AddTransient<NotificationProcessingService>();
        services.AddHostedService<EmailNotificationRequestedEventConsumer>();
        return services;
    }
}
