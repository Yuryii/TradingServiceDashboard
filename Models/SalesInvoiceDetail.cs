using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesInvoiceDetails")]
public class SalesInvoiceDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InvoiceDetailID { get; set; }

    [Required]
    public int InvoiceID { get; set; }

    [Required]
    public int ProductID { get; set; }

    public int? SalesOrderDetailID { get; set; }

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

    [ForeignKey("InvoiceID")]
    public SalesInvoice? Invoice { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("SalesOrderDetailID")]
    public SalesOrderDetail? SalesOrderDetail { get; set; }

    public ICollection<SalesReturnDetail> ReturnDetails { get; set; } = new List<SalesReturnDetail>();
}
