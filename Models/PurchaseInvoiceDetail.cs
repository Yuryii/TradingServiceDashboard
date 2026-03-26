using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("PurchaseInvoiceDetails")]
public class PurchaseInvoiceDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InvoiceDetailID { get; set; }

    [Required]
    public int InvoiceID { get; set; }

    [Required]
    public int ProductID { get; set; }

    public int? PurchaseOrderDetailID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountPercent { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [ForeignKey("InvoiceID")]
    public PurchaseInvoice? Invoice { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("PurchaseOrderDetailID")]
    public PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
}
