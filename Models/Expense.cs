using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Expenses")]
public class Expense
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExpenseID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ExpenseNumber { get; set; } = string.Empty;

    [Required]
    public int EmployeeID { get; set; }

    public int? BranchID { get; set; }

    public int? CategoryID { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ReceiptPath { get; set; }

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
    [InverseProperty("Expenses")]
    public Employee? Employee { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("CategoryID")]
    public ExpenseCategory? Category { get; set; }

    [ForeignKey("ApprovedByEmployeeID")]
    [InverseProperty("ExpensesApproved")]
    public Employee? ApprovedByEmployee { get; set; }
}
