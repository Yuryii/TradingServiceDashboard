using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Quotes")]
public class Quote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int QuoteID { get; set; }

    [Required]
    [MaxLength(20)]
    public string QuoteNumber { get; set; } = string.Empty;

    public int? OpportunityID { get; set; }

    public int? CustomerID { get; set; }

    public int? BranchID { get; set; }

    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; } = 0;

    [MaxLength(500)]
    public string? TermsAndConditions { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("OpportunityID")]
    public Opportunity? Opportunity { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("SalesEmployeeID")]
    public Employee? SalesEmployee { get; set; }
}
