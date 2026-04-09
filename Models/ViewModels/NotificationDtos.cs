using System.ComponentModel.DataAnnotations;

namespace Dashboard.Models.ViewModels;

public class NotificationSignalDto
{
    public int NotificationID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string IconClass { get; set; } = "bx-bell";
    public string IconBgClass { get; set; } = "bg-label-primary";
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class NotificationBadgeDto
{
    public int TotalUnread { get; set; }
}

/// <summary>Aggregated counts for notification UI (navbar + full page summary).</summary>
public class NotificationCountsDto
{
    public int Unread { get; set; }
    public int Total { get; set; }
    public int Critical { get; set; }
    public int Today { get; set; }
}

public class NotificationListItemDto
{
    public int NotificationID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class NotificationConfigListVM
{
    public int ConfigID { get; set; }
    public string NotificationCode { get; set; } = string.Empty;
    public string NotificationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int CheckIntervalMinutes { get; set; }
    public decimal? ThresholdValue { get; set; }
    public decimal? ThresholdValue2 { get; set; }
    public int? DelayHours { get; set; }
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? Description { get; set; }
    public string AllowedRoles { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
}

public class NotificationConfigEditVM
{
    public int ConfigID { get; set; }

    [MaxLength(100)]
    public string NotificationCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string NotificationName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 5;

    public decimal? ThresholdValue { get; set; }

    public decimal? ThresholdValue2 { get; set; }

    public int? DelayHours { get; set; }

    [MaxLength(500)]
    public string AllowedRoles { get; set; } = "*";

    [MaxLength(50)]
    public string IconClass { get; set; } = "bx-bell";

    [MaxLength(50)]
    public string IconBgClass { get; set; } = "bg-label-warning";

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? CronExpression { get; set; }
}

public class JobConfigDto
{
    public int ConfigID { get; set; }
    public string NotificationCode { get; set; } = string.Empty;
    public string NotificationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? CronExpression { get; set; }
    public string? Description { get; set; }
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = string.Empty;
    public int CheckIntervalMinutes { get; set; }
    public decimal? ThresholdValue { get; set; }
    public int? DelayHours { get; set; }
}
