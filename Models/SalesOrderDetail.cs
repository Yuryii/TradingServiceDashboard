using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesOrderDetails")]
public class SalesOrderDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OrderDetailID { get; set; }

    [Required]
    public int SalesOrderID { get; set; }

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
    public decimal QuantityShipped { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityInvoiced { get; set; } = 0;

    [MaxLength(200)]
    public string? Description { get; set; }

    [ForeignKey("SalesOrderID")]
    public SalesOrder? SalesOrder { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    public ICollection<SalesInvoiceDetail> InvoiceDetails { get; set; } = new List<SalesInvoiceDetail>();
    public ICollection<SalesReturnDetail> ReturnDetails { get; set; } = new List<SalesReturnDetail>();
}
