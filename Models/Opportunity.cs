using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Opportunities")]
public class Opportunity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OpportunityID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OpportunityCode { get; set; } = string.Empty;

    public int? CustomerID { get; set; }

    public long? LeadID { get; set; }

    [Required]
    public int OwnerEmployeeID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    public int StageID { get; set; }

    [MaxLength(100)]
    public string? SourceChannel { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }

    public DateTime? ActualCloseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Probability { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [MaxLength(255)]
    public string? WonReason { get; set; }

    [MaxLength(255)]
    public string? LostReason { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Customer? Customer { get; set; }

    public MarketingLead? Lead { get; set; }

    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<OpportunityStageHistory> StageHistory { get; set; } = new List<OpportunityStageHistory>();
}
