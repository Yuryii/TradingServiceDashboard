using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Inventories")]
public class Inventory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InventoryID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int WarehouseID { get; set; }

    [Required]
    public int BranchID { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityOnHand { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityReserved { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityAvailable { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ReorderPoint { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ReorderQuantity { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AverageCost { get; set; } = 0;

    [Required]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    public ICollection<InventorySnapshot> Snapshots { get; set; } = new List<InventorySnapshot>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
