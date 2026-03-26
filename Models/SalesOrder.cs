using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesOrders")]
public class SalesOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SalesOrderID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    public int? OpportunityID { get; set; }

    public int? QuoteID { get; set; }

    public int? CustomerID { get; set; }

    public int? BranchID { get; set; }

    public int? SalesChannelID { get; set; }

    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    [MaxLength(255)]
    public string? ShippingAddress { get; set; }

    [MaxLength(100)]
    public string? ShippingCity { get; set; }

    [MaxLength(100)]
    public string? ShippingProvince { get; set; }

    [MaxLength(100)]
    public string? ShippingCountry { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Opportunity? Opportunity { get; set; }

    public Quote? Quote { get; set; }

    public Customer? Customer { get; set; }

    public Branch? Branch { get; set; }

    public SalesChannel? SalesChannel { get; set; }

    public Employee? SalesEmployee { get; set; }

    public ICollection<SalesOrderDetail> OrderDetails { get; set; } = new List<SalesOrderDetail>();
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
}
