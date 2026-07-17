using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Fcs.Notifications.Application.Settings;

[ExcludeFromCodeCoverage]
public sealed class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    [Required]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required]
    public string GroupId { get; init; } = string.Empty;

    [Range(1, 60000)]
    public int ConsumerTimeoutMs { get; init; } = 100;

    public KafkaTopics Topics { get; init; } = new();
}

[ExcludeFromCodeCoverage]
public sealed class KafkaTopics
{
    [Required]
    public string EmailNotification { get; init; } = string.Empty;
}
