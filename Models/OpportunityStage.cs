using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("OpportunityStages")]
public class OpportunityStage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StageID { get; set; }

    [Required]
    [MaxLength(20)]
    public string StageCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string StageName { get; set; } = string.Empty;

    [Required]
    public int StageOrder { get; set; }

    [Required]
    public bool IsClosedStage { get; set; } = false;

    [Required]
    public bool IsWonStage { get; set; } = false;

    [Required]
    public bool IsLostStage { get; set; } = false;

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    
    [InverseProperty("FromStage")]
    public ICollection<OpportunityStageHistory> StageHistoriesFrom { get; set; } = new List<OpportunityStageHistory>();
    
    [InverseProperty("ToStage")]
    public ICollection<OpportunityStageHistory> StageHistoriesTo { get; set; } = new List<OpportunityStageHistory>();
}
