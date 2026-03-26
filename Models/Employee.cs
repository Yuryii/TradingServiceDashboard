using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dashboard.Models;

[Table("Employees")]
public class Employee
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EmployeeID { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [Required]
    public int DepartmentID { get; set; }

    [Required]
    public int PositionID { get; set; }

    public int? BranchID { get; set; }

    public int? ManagerID { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmploymentType { get; set; } = "FullTime";

    [Required]
    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    [MaxLength(255)]
    public string? TerminationReason { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; } = 0;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("DepartmentID")]
    public Department? Department { get; set; }

    [ForeignKey("PositionID")]
    public Position? Position { get; set; }

    [ForeignKey("BranchID")]
    public Branch? Branch { get; set; }

    [ForeignKey("ManagerID")]
    public Employee? Manager { get; set; }

    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    public ICollection<MarketingLead> AssignedLeads { get; set; } = new List<MarketingLead>();
    public ICollection<Opportunity> OwnedOpportunities { get; set; } = new List<Opportunity>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    [InverseProperty("RequestedByEmployee")]
    public ICollection<PurchaseOrder> PurchaseOrdersRequested { get; set; } = new List<PurchaseOrder>();
    
    [InverseProperty("ApprovedByEmployee")]
    public ICollection<PurchaseOrder> PurchaseOrdersApproved { get; set; } = new List<PurchaseOrder>();
    
    [InverseProperty("Employee")]
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    
    [InverseProperty("ApprovedByEmployee")]
    public ICollection<Expense> ExpensesApproved { get; set; } = new List<Expense>();
    
    public ICollection<KpiTarget> KpiTargets { get; set; } = new List<KpiTarget>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    
    [InverseProperty("Employee")]
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    
    [InverseProperty("ApprovedByEmployee")]
    public ICollection<LeaveRequest> LeaveRequestsApproved { get; set; } = new List<LeaveRequest>();
    
    [InverseProperty("Employee")]
    public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
    
    [InverseProperty("ReviewedByEmployee")]
    public ICollection<PerformanceReview> PerformanceReviewsGiven { get; set; } = new List<PerformanceReview>();
    
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    public ICollection<JobOpening> JobOpenings { get; set; } = new List<JobOpening>();
    public ICollection<Applicant> Applicants { get; set; } = new List<Applicant>();
    public ICollection<SupportTicket> AssignedTickets { get; set; } = new List<SupportTicket>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<OpportunityStageHistory> StageHistoryChanges { get; set; } = new List<OpportunityStageHistory>();
}
