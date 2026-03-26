using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SupplierPayments")]
public class SupplierPayment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = string.Empty;

    public int? PurchaseOrderID { get; set; }

    public int? ReceiptID { get; set; }

    public int? SupplierID { get; set; }

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

    [ForeignKey("PurchaseOrderID")]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("ReceiptID")]
    public PurchaseReceipt? Receipt { get; set; }

    [ForeignKey("SupplierID")]
    public Supplier? Supplier { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("ProcessedByEmployeeID")]
    public Employee? ProcessedByEmployee { get; set; }
}
