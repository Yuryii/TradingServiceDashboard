using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SupplierID { get; set; }

    [Required]
    [MaxLength(20)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string SupplierType { get; set; } = string.Empty;

    public int? RegionID { get; set; }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public int PaymentTermDays { get; set; } = 30;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("RegionID")]
    public Region? Region { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
