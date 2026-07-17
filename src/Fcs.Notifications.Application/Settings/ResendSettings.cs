using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Fcs.Notifications.Application.Settings;

[ExcludeFromCodeCoverage]
public sealed class ResendSettings
{
    public const string SectionName = "Resend";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class ResendTemplatesSettings
{
    public const string SectionName = "ResendTemplates";

    [Required]
    public string DonorWelcomeTemplateId { get; init; } = string.Empty;

    [Required]
    public string DonationCreatedTemplateId { get; init; } = string.Empty;

    [Required]
    public string DonationProcessedTemplateId { get; init; } = string.Empty;
}
