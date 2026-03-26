using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesInvoices")]
public class SalesInvoice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InvoiceID { get; set; }

    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? SalesOrderID { get; set; }

    public int? CustomerID { get; set; }

    public int? BranchID { get; set; }

    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

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

    [Column(TypeName = "decimal(18,2)")]
    public decimal OutstandingAmount { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("SalesOrderID")]
    public SalesOrder? SalesOrder { get; set; }

    public Customer? Customer { get; set; }

    public Branch? Branch { get; set; }

    public Employee? SalesEmployee { get; set; }

    public ICollection<SalesInvoiceDetail> InvoiceDetails { get; set; } = new List<SalesInvoiceDetail>();
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
    public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
}
