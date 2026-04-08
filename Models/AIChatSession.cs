using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

[Table("AIChatSessions")]
public class AIChatSession
{
    [Key]
    public int SessionId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
}
