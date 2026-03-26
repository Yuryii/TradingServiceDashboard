using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("ProductCategories")]
public class ProductCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    public int? ParentCategoryID { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [ForeignKey("ParentCategoryID")]
    public ProductCategory? ParentCategory { get; set; }

    public ProductCategory? SubCategories { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
