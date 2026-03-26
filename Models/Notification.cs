using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Notifications")]
public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NotificationID { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Info";

    [MaxLength(50)]
    public string IconClass { get; set; } = "bx-bell";

    [MaxLength(50)]
    public string IconBgClass { get; set; } = "bg-label-primary";

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public int? ReferenceId { get; set; }

    [MaxLength(100)]
    public string? ReferenceType { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }
}
