using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("StockTransactions")]
public class StockTransaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long TransactionID { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionNumber { get; set; } = string.Empty;

    [Required]
    public int ProductID { get; set; }

    public int? WarehouseID { get; set; }

    public int? BranchID { get; set; }

    public int? EmployeeID { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public int? ReferenceID { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("EmployeeID")]
    public Employee? Employee { get; set; }
}
