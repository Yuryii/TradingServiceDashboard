using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("MarketingLeads")]
public class MarketingLead
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long LeadID { get; set; }

    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    public int? CampaignID { get; set; }

    [Required]
    [MaxLength(200)]
    public string LeadName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";

    public int? LeadScore { get; set; }

    public int? AssignedEmployeeID { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? MQLDate { get; set; }

    public DateTime? SQLDate { get; set; }

    public DateTime? ConvertedDate { get; set; }

    public int? ConvertedCustomerID { get; set; }

    [MaxLength(255)]
    public string? LostReason { get; set; }

    [MaxLength(100)]
    public string? UtmSource { get; set; }

    [MaxLength(100)]
    public string? UtmMedium { get; set; }

    [MaxLength(150)]
    public string? UtmCampaign { get; set; }

    [ForeignKey("CampaignID")]
    public MarketingCampaign? Campaign { get; set; }

    [ForeignKey("AssignedEmployeeID")]
    public Employee? AssignedEmployee { get; set; }

    [ForeignKey("ConvertedCustomerID")]
    public Customer? ConvertedCustomer { get; set; }

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
