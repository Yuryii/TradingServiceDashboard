using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesReturns")]
public class SalesReturn
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReturnID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReturnNumber { get; set; } = string.Empty;

    public int? SalesOrderID { get; set; }

    public int? InvoiceID { get; set; }

    public int? CustomerID { get; set; }

    public int? BranchID { get; set; }

    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("SalesOrderID")]
    public SalesOrder? SalesOrder { get; set; }

    [ForeignKey("InvoiceID")]
    public SalesInvoice? Invoice { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("ProcessedByEmployeeID")]
    public Employee? ProcessedByEmployee { get; set; }

    public ICollection<SalesReturnDetail> ReturnDetails { get; set; } = new List<SalesReturnDetail>();
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
}
