using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseOrderDetails")]
public class PurchaseOrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OrderDetailID { get; set; }

    [Required]
    public int PurchaseOrderID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountPercent { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityReceived { get; set; } = 0;

    [MaxLength(200)]
    public string? Description { get; set; }

    [ForeignKey("PurchaseOrderID")]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    public ICollection<PurchaseReceiptDetail> ReceiptDetails { get; set; } = new List<PurchaseReceiptDetail>();
}
