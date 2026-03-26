using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Dim_Date")]
public class DimDate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DateKey { get; set; }

    [Required]
    public DateTime FullDate { get; set; }

    [Required]
    public byte DayNumberOfMonth { get; set; }

    [Required]
    [MaxLength(20)]
    public string DayName { get; set; } = string.Empty;

    [Required]
    public byte WeekNumberOfYear { get; set; }

    [Required]
    public byte MonthNumber { get; set; }

    [Required]
    [MaxLength(20)]
    public string MonthName { get; set; } = string.Empty;

    [Required]
    public byte QuarterNumber { get; set; }

    [Required]
    public short YearNumber { get; set; }

    [Required]
    [MaxLength(7)]
    public string YearMonth { get; set; } = string.Empty;

    [Required]
    public bool IsWeekend { get; set; }

    [Required]
    public bool IsMonthEnd { get; set; }

    [Required]
    public bool IsQuarterEnd { get; set; }

    [Required]
    public bool IsYearEnd { get; set; }
}
