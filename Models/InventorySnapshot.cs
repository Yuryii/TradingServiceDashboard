using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("InventorySnapshots")]
public class InventorySnapshot
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long SnapshotID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int WarehouseID { get; set; }

    [Required]
    public int BranchID { get; set; }

    [Required]
    public DateTime SnapshotDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityOnHand { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AverageCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }
}
