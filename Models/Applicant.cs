using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Applicants")]
public class Applicant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ApplicantID { get; set; }

    [Required]
    public int JobOpeningID { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ResumePath { get; set; }

    [MaxLength(500)]
    public string? CoverLetter { get; set; }

    [MaxLength(50)]
    public string? LinkedInProfile { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Applied";

    [Required]
    public DateTime AppliedDate { get; set; }

    public DateTime? InterviewDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public int ReferredByEmployeeID { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("JobOpeningID")]
    [InverseProperty("Applicants")]
    public JobOpening? JobOpening { get; set; }

    [ForeignKey("ReferredByEmployeeID")]
    [InverseProperty("Applicants")]
    public Employee? ReferredByEmployee { get; set; }
}
