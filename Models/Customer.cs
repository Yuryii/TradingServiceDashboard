using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Customers")]
public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CustomerID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CustomerType { get; set; } = string.Empty;

    public int? CustomerGroupID { get; set; }

    public int? RegionID { get; set; }

    public int? BranchID { get; set; }

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public DateTime? JoinDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;

    public int PaymentTermDays { get; set; } = 30;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CustomerGroupID")]
    public CustomerGroup? CustomerGroup { get; set; }

    [ForeignKey("RegionID")]
    public Region? Region { get; set; }

    public Branch? Branch { get; set; }

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<MarketingLead> MarketingLeads { get; set; } = new List<MarketingLead>();
}
