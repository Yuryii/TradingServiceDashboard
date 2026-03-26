using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("OpportunityStageHistory")]
public class OpportunityStageHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long HistoryID { get; set; }

    [Required]
    public int OpportunityID { get; set; }

    public int? FromStageID { get; set; }

    [Required]
    public int ToStageID { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public int? ChangedByEmployeeID { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }

    [ForeignKey("OpportunityID")]
    public Opportunity? Opportunity { get; set; }

    [ForeignKey("FromStageID")]
    [InverseProperty("StageHistoriesFrom")]
    public OpportunityStage? FromStage { get; set; }

    [ForeignKey("ToStageID")]
    [InverseProperty("StageHistoriesTo")]
    public OpportunityStage? ToStage { get; set; }

    [ForeignKey("ChangedByEmployeeID")]
    [InverseProperty("StageHistoryChanges")]
    public Employee? ChangedByEmployee { get; set; }
}
