using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("CustomerGroups")]
public class CustomerGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CustomerGroupID { get; set; }

    [Required]
    [MaxLength(20)]
    public string GroupCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
