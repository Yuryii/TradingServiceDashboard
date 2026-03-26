using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseOrders")]
public class PurchaseOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PurchaseOrderID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    public int? SupplierID { get; set; }

    public int? WarehouseID { get; set; }

    public int? BranchID { get; set; }

    public int? RequestedByEmployeeID { get; set; }

    public int? ApprovedByEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("SupplierID")]
    public Supplier? Supplier { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("RequestedByEmployeeID")]
    [InverseProperty("PurchaseOrdersRequested")]
    public Employee? RequestedByEmployee { get; set; }

    [ForeignKey("ApprovedByEmployeeID")]
    [InverseProperty("PurchaseOrdersApproved")]
    public Employee? ApprovedByEmployee { get; set; }

    public ICollection<PurchaseOrderDetail> OrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    public ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
