using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductID { get; set; }

    [Required]
    [MaxLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int? CategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProductType { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; } = 0;

    public int ReorderLevel { get; set; } = 0;

    public int? MaxStockLevel { get; set; }

    [Required]
    public bool IsStockItem { get; set; } = true;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryID")]
    public ProductCategory? Category { get; set; }

    public ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();
    public ICollection<SalesReturnDetail> SalesReturnDetails { get; set; } = new List<SalesReturnDetail>();
    public ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<InventorySnapshot> InventorySnapshots { get; set; } = new List<InventorySnapshot>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
