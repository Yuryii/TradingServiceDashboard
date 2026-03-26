using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesChannels")]
public class SalesChannel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SalesChannelID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ChannelCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ChannelName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
