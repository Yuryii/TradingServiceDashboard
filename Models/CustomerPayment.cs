using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("CustomerPayments")]
public class CustomerPayment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = string.Empty;

    public int? SalesOrderID { get; set; }

    public int? InvoiceID { get; set; }

    public int? ReturnID { get; set; }

    public int? CustomerID { get; set; }

    public int? BranchID { get; set; }

    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("SalesOrderID")]
    public SalesOrder? SalesOrder { get; set; }

    [ForeignKey("InvoiceID")]
    public SalesInvoice? Invoice { get; set; }

    [ForeignKey("ReturnID")]
    public SalesReturn? SalesReturn { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("ProcessedByEmployeeID")]
    public Employee? ProcessedByEmployee { get; set; }
}
