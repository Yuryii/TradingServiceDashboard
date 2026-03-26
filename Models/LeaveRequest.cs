using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("LeaveRequests")]
public class LeaveRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LeaveRequestID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalDays { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public int? ApprovedByEmployeeID { get; set; }

    public DateTime? ApprovedDate { get; set; }

    [MaxLength(255)]
    public string? RejectionReason { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("EmployeeID")]
    [InverseProperty("LeaveRequests")]
    public Employee? Employee { get; set; }

    [ForeignKey("ApprovedByEmployeeID")]
    [InverseProperty("LeaveRequestsApproved")]
    public Employee? ApprovedByEmployee { get; set; }
}
