using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Regions")]
public class Region
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RegionID { get; set; }

    [Required]
    [MaxLength(20)]
    public string RegionCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string RegionName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
