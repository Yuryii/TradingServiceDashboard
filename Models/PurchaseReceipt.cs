using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseReceipts")]
public class PurchaseReceipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReceiptID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public int? PurchaseOrderID { get; set; }

    public int? SupplierID { get; set; }

    public int? WarehouseID { get; set; }

    public int? BranchID { get; set; }

    public int? ReceivedByEmployeeID { get; set; }

    [Required]
    public DateTime ReceiptDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

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

    [ForeignKey("ReceivedByEmployeeID")]
    public Employee? ReceivedByEmployee { get; set; }

    public ICollection<PurchaseReceiptDetail> ReceiptDetails { get; set; } = new List<PurchaseReceiptDetail>();
}
