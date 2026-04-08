using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("AIChatMessages")]
public class AIChatMessage
{
    [Key]
    public int MessageId { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    public ChatRole Role { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Metadata { get; set; }

    [ForeignKey(nameof(SessionId))]
    public virtual AIChatSession? Session { get; set; }
}
