using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("JobOpenings")]
public class JobOpening
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int JobOpeningID { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public int DepartmentID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmploymentType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Location { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMax { get; set; }

    [Required]
    public int NumberOfPositions { get; set; }

    [MaxLength(1000)]
    public string? JobDescription { get; set; }

    [MaxLength(1000)]
    public string? Requirements { get; set; }

    [Required]
    public DateTime PostedDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [Required]
    public int CreatedByEmployeeID { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("DepartmentID")]
    public Department? Department { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("CreatedByEmployeeID")]
    [InverseProperty("JobOpenings")]
    public Employee? CreatedByEmployee { get; set; }

    public ICollection<Applicant> Applicants { get; set; } = new List<Applicant>();
}
