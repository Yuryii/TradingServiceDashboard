using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PerformanceReviews")]
public class PerformanceReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReviewID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    public int? ReviewedByEmployeeID { get; set; }

    [Required]
    public DateTime ReviewDate { get; set; }

    [Required]
    public DateTime ReviewPeriodStart { get; set; }

    [Required]
    public DateTime ReviewPeriodEnd { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? OverallRating { get; set; }

    [MaxLength(1000)]
    public string? Strengths { get; set; }

    [MaxLength(1000)]
    public string? AreasForImprovement { get; set; }

    [MaxLength(1000)]
    public string? Comments { get; set; }

    [MaxLength(500)]
    public string? Goals { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("EmployeeID")]
    [InverseProperty("PerformanceReviews")]
    public Employee? Employee { get; set; }

    [ForeignKey("ReviewedByEmployeeID")]
    [InverseProperty("PerformanceReviewsGiven")]
    public Employee? ReviewedByEmployee { get; set; }
}
