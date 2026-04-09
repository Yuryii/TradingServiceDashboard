using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("NotificationConfigs")]
public class NotificationConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ConfigID { get; set; }

    [Required]
    [MaxLength(100)]
    public string NotificationCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NotificationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Info";

    public bool IsEnabled { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 5;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ThresholdValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ThresholdValue2 { get; set; }

    public int? DelayHours { get; set; }

    [MaxLength(50)]
    public string IconClass { get; set; } = "bx-bell";

    [MaxLength(50)]
    public string IconBgClass { get; set; } = "bg-label-warning";

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string AllowedRoles { get; set; } = "*";

    [MaxLength(50)]
    public string? CronExpression { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
