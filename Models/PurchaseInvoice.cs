using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseInvoices")]
public class PurchaseInvoice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InvoiceID { get; set; }

    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? PurchaseOrderID { get; set; }

    public int? SupplierID { get; set; }

    public int? WarehouseID { get; set; }

    public int? BranchID { get; set; }

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
    public decimal AmountPaid { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountDue { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("PurchaseOrderID")]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("SupplierID")]
    public Supplier? Supplier { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    public ICollection<PurchaseInvoiceDetail> InvoiceDetails { get; set; } = new List<PurchaseInvoiceDetail>();
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
