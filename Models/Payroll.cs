using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Payrolls")]
public class Payroll
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PayrollID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public int BranchID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PayrollPeriod { get; set; } = string.Empty;

    [Required]
    public DateTime PeriodStartDate { get; set; }

    [Required]
    public DateTime PeriodEndDate { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BonusAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AllowanceAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeductionAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetSalary { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("EmployeeID")]
    public Employee? Employee { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }
}
