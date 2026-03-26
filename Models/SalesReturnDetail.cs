using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("SalesReturnDetails")]
public class SalesReturnDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReturnDetailID { get; set; }

    [Required]
    public int ReturnID { get; set; }

    [Required]
    public int ProductID { get; set; }

    public int? InvoiceDetailID { get; set; }

    public int? SalesOrderDetailID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    [ForeignKey("ReturnID")]
    public SalesReturn? SalesReturn { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("InvoiceDetailID")]
    public SalesInvoiceDetail? InvoiceDetail { get; set; }

    [ForeignKey("SalesOrderDetailID")]
    public SalesOrderDetail? SalesOrderDetail { get; set; }
}
