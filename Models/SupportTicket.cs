using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SupportTickets")]
public class SupportTicket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TicketID { get; set; }

    [Required]
    [MaxLength(20)]
    public string TicketNumber { get; set; } = string.Empty;

    [Required]
    public int CustomerID { get; set; }

    public int? AssignedToEmployeeID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TicketType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(1000)]
    public string? Resolution { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    [ForeignKey("AssignedToEmployeeID")]
    public Employee? AssignedToEmployee { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }
}
