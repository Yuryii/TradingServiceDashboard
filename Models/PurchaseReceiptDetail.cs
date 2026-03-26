using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseReceiptDetails")]
public class PurchaseReceiptDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReceiptDetailID { get; set; }

    [Required]
    public int ReceiptID { get; set; }

    [Required]
    public int ProductID { get; set; }

    public int? PurchaseOrderDetailID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("ReceiptID")]
    public PurchaseReceipt? Receipt { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("PurchaseOrderDetailID")]
    public PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
}
