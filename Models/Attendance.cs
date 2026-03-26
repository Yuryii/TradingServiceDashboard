using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Attendances")]
public class Attendance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AttendanceID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Present";

    [MaxLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("EmployeeID")]
    public Employee? Employee { get; set; }
}
