using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("MarketingCampaigns")]
public class MarketingCampaign
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CampaignID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CampaignCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Channel { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Budget { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualSpend { get; set; } = 0;

    [MaxLength(255)]
    public string? Objective { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Planned";

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<MarketingLead> MarketingLeads { get; set; } = new List<MarketingLead>();
    public ICollection<MarketingSpendDaily> DailySpends { get; set; } = new List<MarketingSpendDaily>();
}
