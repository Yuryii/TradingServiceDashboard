using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("MarketingSpendDailies")]
public class MarketingSpendDaily
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long SpendID { get; set; }

    [Required]
    public int CampaignID { get; set; }

    [Required]
    public DateTime SpendDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Impressions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Clicks { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Conversions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CPM { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CPC { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CPA { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("CampaignID")]
    public MarketingCampaign? Campaign { get; set; }
}
