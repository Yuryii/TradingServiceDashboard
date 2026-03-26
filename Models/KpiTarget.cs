using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("KpiTargets")]
public class KpiTarget
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int KpiTargetID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    public int? DepartmentID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    [MaxLength(100)]
    public string KpiName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string KpiType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ActualValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? AchievementPercent { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("EmployeeID")]
    public Employee? Employee { get; set; }

    [ForeignKey("DepartmentID")]
    public Department? Department { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }
}
