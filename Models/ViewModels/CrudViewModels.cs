using System.ComponentModel.DataAnnotations;

namespace Dashboard.Models.ViewModels;

// ============================================================
// SALES DEPARTMENT VIEW MODELS
// ============================================================

#region Customer

public class CustomerListVM
{
    public int CustomerID { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerType { get; set; } = "";
    public string? CustomerGroupName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerCreateVM
{
    [Required]
    [MaxLength(20)]
    public string CustomerCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string CustomerType { get; set; } = "";

    public int? CustomerGroupID { get; set; }
    public int? RegionID { get; set; }
    public int? BranchID { get; set; }

    [MaxLength(100)]
    public string? Industry { get; set; }

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

    public DateTime? JoinDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CreditLimit { get; set; }

    [Range(0, 365)]
    public int PaymentTermDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;
}

public class CustomerEditVM
{
    public int CustomerID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CustomerCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string CustomerType { get; set; } = "";

    public int? CustomerGroupID { get; set; }
    public int? RegionID { get; set; }
    public int? BranchID { get; set; }

    [MaxLength(100)]
    public string? Industry { get; set; }

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

    public DateTime? JoinDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CreditLimit { get; set; }

    [Range(0, 365)]
    public int PaymentTermDays { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region CustomerGroup

public class CustomerGroupListVM
{
    public int CustomerGroupID { get; set; }
    public string GroupCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CustomerGroupCreateVM
{
    [Required]
    [MaxLength(20)]
    public string GroupCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CustomerGroupEditVM
{
    public int CustomerGroupID { get; set; }

    [Required]
    [MaxLength(20)]
    public string GroupCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

#endregion

#region SalesChannel

public class SalesChannelListVM
{
    public int SalesChannelID { get; set; }
    public string ChannelCode { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SalesChannelCreateVM
{
    [Required]
    [MaxLength(20)]
    public string ChannelCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string ChannelName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SalesChannelEditVM
{
    public int SalesChannelID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ChannelCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string ChannelName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

#endregion

#region OpportunityStage

public class OpportunityStageListVM
{
    public int StageID { get; set; }
    public string StageCode { get; set; } = "";
    public string StageName { get; set; } = "";
    public int StageOrder { get; set; }
    public bool IsClosedStage { get; set; }
    public bool IsWonStage { get; set; }
    public bool IsLostStage { get; set; }
}

public class OpportunityStageCreateVM
{
    [Required]
    [MaxLength(20)]
    public string StageCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string StageName { get; set; } = "";

    [Required]
    public int StageOrder { get; set; }

    public bool IsClosedStage { get; set; }
    public bool IsWonStage { get; set; }
    public bool IsLostStage { get; set; }
}

public class OpportunityStageEditVM
{
    public int StageID { get; set; }

    [Required]
    [MaxLength(20)]
    public string StageCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string StageName { get; set; } = "";

    [Required]
    public int StageOrder { get; set; }

    public bool IsClosedStage { get; set; }
    public bool IsWonStage { get; set; }
    public bool IsLostStage { get; set; }
}

#endregion

#region Opportunity

public class OpportunityListVM
{
    public int OpportunityID { get; set; }
    public string OpportunityCode { get; set; } = "";
    public string? CustomerName { get; set; }
    public int StageID { get; set; }
    public string? StageName { get; set; }
    public string Status { get; set; } = "";
    public decimal EstimatedValue { get; set; }
    public decimal Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OpportunityCreateVM
{
    [Required]
    [MaxLength(20)]
    public string OpportunityCode { get; set; } = "";

    public int? CustomerID { get; set; }
    public long? LeadID { get; set; }

    [Required]
    public int OwnerEmployeeID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    public int StageID { get; set; }

    [MaxLength(100)]
    public string? SourceChannel { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ActualCloseDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstimatedValue { get; set; }

    [Range(0, 100)]
    public decimal Probability { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [MaxLength(255)]
    public string? WonReason { get; set; }

    [MaxLength(255)]
    public string? LostReason { get; set; }
}

public class OpportunityEditVM
{
    public int OpportunityID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OpportunityCode { get; set; } = "";

    public int? CustomerID { get; set; }
    public long? LeadID { get; set; }

    [Required]
    public int OwnerEmployeeID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    public int StageID { get; set; }

    [MaxLength(100)]
    public string? SourceChannel { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ActualCloseDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstimatedValue { get; set; }

    [Range(0, 100)]
    public decimal Probability { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(255)]
    public string? WonReason { get; set; }

    [MaxLength(255)]
    public string? LostReason { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Quote

public class QuoteListVM
{
    public int QuoteID { get; set; }
    public string QuoteNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? OpportunityCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntilDate { get; set; }
}

public class QuoteCreateVM
{
    [Required]
    [MaxLength(20)]
    public string QuoteNumber { get; set; } = "";

    public int? OpportunityID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [MaxLength(500)]
    public string? TermsAndConditions { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
}

public class QuoteEditVM
{
    public int QuoteID { get; set; }

    [Required]
    [MaxLength(20)]
    public string QuoteNumber { get; set; } = "";

    public int? OpportunityID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; }

    public DateTime? ValidUntilDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [MaxLength(500)]
    public string? TermsAndConditions { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

#endregion

#region SalesOrder

public class SalesOrderListVM
{
    public int SalesOrderID { get; set; }
    public string OrderNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? OpportunityCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string DeliveryStatus { get; set; } = "";
    public DateTime OrderDate { get; set; }
}

public class SalesOrderCreateVM
{
    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = "";

    public int? OpportunityID { get; set; }
    public int? QuoteID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesChannelID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    [MaxLength(255)]
    public string? ShippingAddress { get; set; }

    [MaxLength(100)]
    public string? ShippingCity { get; set; }

    [MaxLength(100)]
    public string? ShippingProvince { get; set; }

    [MaxLength(100)]
    public string? ShippingCountry { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SalesOrderEditVM
{
    public int SalesOrderID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = "";

    public int? OpportunityID { get; set; }
    public int? QuoteID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesChannelID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "";

    [MaxLength(255)]
    public string? ShippingAddress { get; set; }

    [MaxLength(100)]
    public string? ShippingCity { get; set; }

    [MaxLength(100)]
    public string? ShippingProvince { get; set; }

    [MaxLength(100)]
    public string? ShippingCountry { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region SalesInvoice

public class SalesInvoiceListVM
{
    public int InvoiceID { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? SalesOrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
}

public class SalesInvoiceCreateVM
{
    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OutstandingAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SalesInvoiceEditVM
{
    public int InvoiceID { get; set; }

    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? SalesEmployeeID { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PaidAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OutstandingAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region SalesReturn

public class SalesReturnListVM
{
    public int ReturnID { get; set; }
    public string ReturnNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? SalesOrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReturnDate { get; set; }
}

public class SalesReturnCreateVM
{
    [Required]
    [MaxLength(20)]
    public string ReturnNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? InvoiceID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SalesReturnEditVM
{
    public int ReturnID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReturnNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? InvoiceID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region CustomerPayment

public class CustomerPaymentListVM
{
    public int PaymentID { get; set; }
    public string PaymentNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateTime PaymentDate { get; set; }
}

public class CustomerPaymentCreateVM
{
    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? InvoiceID { get; set; }
    public int? ReturnID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }
}

public class CustomerPaymentEditVM
{
    public int PaymentID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = "";

    public int? SalesOrderID { get; set; }
    public int? InvoiceID { get; set; }
    public int? ReturnID { get; set; }
    public int? CustomerID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

// ============================================================
// MARKETING DEPARTMENT VIEW MODELS
// ============================================================

#region MarketingCampaign

public class MarketingCampaignListVM
{
    public int CampaignID { get; set; }
    public string CampaignCode { get; set; } = "";
    public string CampaignName { get; set; } = "";
    public string Channel { get; set; } = "";
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class MarketingCampaignCreateVM
{
    [Required]
    [MaxLength(20)]
    public string CampaignCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string Channel { get; set; } = "";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Budget { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualSpend { get; set; }

    [MaxLength(255)]
    public string? Objective { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Planned";

    public bool IsActive { get; set; } = true;
}

public class MarketingCampaignEditVM
{
    public int CampaignID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CampaignCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string Channel { get; set; } = "";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Budget { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualSpend { get; set; }

    [MaxLength(255)]
    public string? Objective { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region MarketingLead

public class MarketingLeadListVM
{
    public long LeadID { get; set; }
    public string LeadCode { get; set; } = "";
    public string LeadName { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "";
    public int? LeadScore { get; set; }
    public string? CampaignName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class MarketingLeadCreateVM
{
    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = "";

    public int? CampaignID { get; set; }

    [Required]
    [MaxLength(200)]
    public string LeadName { get; set; } = "";

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";

    public int? LeadScore { get; set; }
    public int? AssignedEmployeeID { get; set; }

    [MaxLength(100)]
    public string? UtmSource { get; set; }

    [MaxLength(100)]
    public string? UtmMedium { get; set; }

    [MaxLength(150)]
    public string? UtmCampaign { get; set; }
}

public class MarketingLeadEditVM
{
    public long LeadID { get; set; }

    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = "";

    public int? CampaignID { get; set; }

    [Required]
    [MaxLength(200)]
    public string LeadName { get; set; } = "";

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public int? LeadScore { get; set; }
    public int? AssignedEmployeeID { get; set; }

    [MaxLength(100)]
    public string? UtmSource { get; set; }

    [MaxLength(100)]
    public string? UtmMedium { get; set; }

    [MaxLength(150)]
    public string? UtmCampaign { get; set; }

    public DateTime CreatedDate { get; set; }
}

#endregion

#region MarketingSpendDaily

public class MarketingSpendDailyListVM
{
    public long SpendID { get; set; }
    public string? CampaignName { get; set; }
    public decimal Amount { get; set; }
    public decimal? Impressions { get; set; }
    public decimal? Clicks { get; set; }
    public decimal? Conversions { get; set; }
    public decimal? CPM { get; set; }
    public decimal? CPC { get; set; }
    public DateTime SpendDate { get; set; }
}

public class MarketingSpendDailyCreateVM
{
    [Required]
    public int CampaignID { get; set; }

    [Required]
    public DateTime SpendDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Impressions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Clicks { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Conversions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPM { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPC { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPA { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class MarketingSpendDailyEditVM
{
    public long SpendID { get; set; }

    [Required]
    public int CampaignID { get; set; }

    [Required]
    public DateTime SpendDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Impressions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Clicks { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Conversions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPM { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPC { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CPA { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

#endregion

// ============================================================
// INVENTORY DEPARTMENT VIEW MODELS
// ============================================================

#region Product

public class ProductListVM
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? CategoryName { get; set; }
    public string ProductType { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public bool IsActive { get; set; }
}

public class ProductCreateVM
{
    [Required]
    [MaxLength(30)]
    public string ProductCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = "";

    public int? CategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProductType { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string UnitOfMeasure { get; set; } = "";

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    public int? MaxStockLevel { get; set; }

    public bool IsStockItem { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class ProductEditVM
{
    public int ProductID { get; set; }

    [Required]
    [MaxLength(30)]
    public string ProductCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = "";

    public int? CategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProductType { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string UnitOfMeasure { get; set; } = "";

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    public int? MaxStockLevel { get; set; }

    public bool IsStockItem { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region ProductCategory

public class ProductCategoryListVM
{
    public int CategoryID { get; set; }
    public string CategoryCode { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string? ParentCategoryName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProductCategoryCreateVM
{
    [Required]
    [MaxLength(20)]
    public string CategoryCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string CategoryName { get; set; } = "";

    public int? ParentCategoryID { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductCategoryEditVM
{
    public int CategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CategoryCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string CategoryName { get; set; } = "";

    public int? ParentCategoryID { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

#endregion

#region Warehouse

public class WarehouseListVM
{
    public int WarehouseID { get; set; }
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? BranchName { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseCreateVM
{
    [Required]
    [MaxLength(20)]
    public string WarehouseCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string WarehouseName { get; set; } = "";

    public int? BranchID { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WarehouseEditVM
{
    public int WarehouseID { get; set; }

    [Required]
    [MaxLength(20)]
    public string WarehouseCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string WarehouseName { get; set; } = "";

    public int? BranchID { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Supplier

public class SupplierListVM
{
    public int SupplierID { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string SupplierType { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierCreateVM
{
    [Required]
    [MaxLength(20)]
    public string SupplierCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string SupplierType { get; set; } = "";

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
    public bool IsActive { get; set; } = true;
}

public class SupplierEditVM
{
    public int SupplierID { get; set; }

    [Required]
    [MaxLength(20)]
    public string SupplierCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string SupplierType { get; set; } = "";

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

    public int PaymentTermDays { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region PurchaseOrder

public class PurchaseOrderListVM
{
    public int PurchaseOrderID { get; set; }
    public string OrderNumber { get; set; } = "";
    public string? SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string DeliveryStatus { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
}

public class PurchaseOrderCreateVM
{
    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = "";

    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }
    public int? RequestedByEmployeeID { get; set; }
    public int? ApprovedByEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
}

public class PurchaseOrderEditVM
{
    public int PurchaseOrderID { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = "";

    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }
    public int? RequestedByEmployeeID { get; set; }
    public int? ApprovedByEmployeeID { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

#endregion

#region PurchaseReceipt

public class PurchaseReceiptListVM
{
    public int ReceiptID { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string? SupplierName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
}

public class PurchaseReceiptCreateVM
{
    [Required]
    [MaxLength(20)]
    public string ReceiptNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }
    public int? ReceivedByEmployeeID { get; set; }

    [Required]
    public DateTime ReceiptDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PurchaseReceiptEditVM
{
    public int ReceiptID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReceiptNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }
    public int? ReceivedByEmployeeID { get; set; }

    [Required]
    public DateTime ReceiptDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region PurchaseInvoice

public class PurchaseInvoiceListVM
{
    public int InvoiceID { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string? SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
}

public class PurchaseInvoiceCreateVM
{
    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountPaid { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountDue { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PurchaseInvoiceEditVM
{
    public int InvoiceID { get; set; }

    [Required]
    [MaxLength(20)]
    public string InvoiceNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? SupplierID { get; set; }
    public int? WarehouseID { get; set; }
    public int? BranchID { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SubTotal { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountPaid { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountDue { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region SupplierPayment

public class SupplierPaymentListVM
{
    public int PaymentID { get; set; }
    public string PaymentNumber { get; set; } = "";
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateTime PaymentDate { get; set; }
}

public class SupplierPaymentCreateVM
{
    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? ReceiptID { get; set; }
    public int? SupplierID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }
}

public class SupplierPaymentEditVM
{
    public int PaymentID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentNumber { get; set; } = "";

    public int? PurchaseOrderID { get; set; }
    public int? ReceiptID { get; set; }
    public int? SupplierID { get; set; }
    public int? BranchID { get; set; }
    public int? ProcessedByEmployeeID { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "";

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

// ============================================================
// FINANCE DEPARTMENT VIEW MODELS
// ============================================================

#region Expense

public class ExpenseListVM
{
    public int ExpenseID { get; set; }
    public string ExpenseNumber { get; set; } = "";
    public string? EmployeeName { get; set; }
    public string? CategoryName { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime ExpenseDate { get; set; }
}

public class ExpenseCreateVM
{
    [Required]
    [MaxLength(20)]
    public string ExpenseNumber { get; set; } = "";

    [Required]
    public int EmployeeID { get; set; }

    public int? BranchID { get; set; }
    public int? CategoryID { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ReceiptPath { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public int? ApprovedByEmployeeID { get; set; }
}

public class ExpenseEditVM
{
    public int ExpenseID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ExpenseNumber { get; set; } = "";

    [Required]
    public int EmployeeID { get; set; }

    public int? BranchID { get; set; }
    public int? CategoryID { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ReceiptPath { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public int? ApprovedByEmployeeID { get; set; }
    public DateTime? ApprovedDate { get; set; }

    [MaxLength(255)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region ExpenseCategory

public class ExpenseCategoryListVM
{
    public int ExpenseCategoryID { get; set; }
    public string CategoryCode { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ExpenseCategoryCreateVM
{
    [Required]
    [MaxLength(20)]
    public string CategoryCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string CategoryName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ExpenseCategoryEditVM
{
    public int ExpenseCategoryID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CategoryCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string CategoryName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

#endregion

// ============================================================
// HUMAN RESOURCES DEPARTMENT VIEW MODELS
// ============================================================

#region Position

public class PositionListVM
{
    public int PositionID { get; set; }
    public string PositionCode { get; set; } = "";
    public string PositionName { get; set; } = "";
    public string PositionLevel { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class PositionCreateVM
{
    [Required]
    [MaxLength(20)]
    public string PositionCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string PositionName { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string PositionLevel { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class PositionEditVM
{
    public int PositionID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PositionCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string PositionName { get; set; } = "";

    [Required]
    [MaxLength(30)]
    public string PositionLevel { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Employee

public class EmployeeListVM
{
    public int EmployeeID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string EmploymentType { get; set; } = "";
    public bool IsActive { get; set; }
}

public class EmployeeCreateVM
{
    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

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

    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmployeeEditVM
{
    public int EmployeeID { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

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
    public string EmploymentType { get; set; } = "";

    [Required]
    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    [MaxLength(255)]
    public string? TerminationReason { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Attendance

public class AttendanceListVM
{
    public int AttendanceID { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

public class AttendanceCreateVM
{
    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Present";

    [MaxLength(255)]
    public string? Notes { get; set; }
}

public class AttendanceEditVM
{
    public int AttendanceID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(255)]
    public string? Notes { get; set; }
}

#endregion

#region LeaveRequest

public class LeaveRequestListVM
{
    public int LeaveRequestID { get; set; }
    public string? EmployeeName { get; set; }
    public string LeaveType { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Status { get; set; } = "";
}

public class LeaveRequestCreateVM
{
    [Required]
    public int EmployeeID { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = "";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalDays { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
}

public class LeaveRequestEditVM
{
    public int LeaveRequestID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = "";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalDays { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public int? ApprovedByEmployeeID { get; set; }
    public DateTime? ApprovedDate { get; set; }

    [MaxLength(255)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Payroll

public class PayrollListVM
{
    public int PayrollID { get; set; }
    public string? EmployeeName { get; set; }
    public string PayrollPeriod { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = "";
}

public class PayrollCreateVM
{
    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public int BranchID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PayrollPeriod { get; set; } = "";

    [Required]
    public DateTime PeriodStartDate { get; set; }

    [Required]
    public DateTime PeriodEndDate { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OvertimeAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BonusAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AllowanceAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DeductionAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal NetSalary { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PayrollEditVM
{
    public int PayrollID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    public int BranchID { get; set; }

    [Required]
    [MaxLength(20)]
    public string PayrollPeriod { get; set; } = "";

    [Required]
    public DateTime PeriodStartDate { get; set; }

    [Required]
    public DateTime PeriodEndDate { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OvertimeAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BonusAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AllowanceAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DeductionAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal NetSalary { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region PerformanceReview

public class PerformanceReviewListVM
{
    public int ReviewID { get; set; }
    public string? EmployeeName { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime ReviewDate { get; set; }
    public decimal? OverallRating { get; set; }
}

public class PerformanceReviewCreateVM
{
    [Required]
    public int EmployeeID { get; set; }

    public int? ReviewedByEmployeeID { get; set; }

    [Required]
    public DateTime ReviewDate { get; set; }

    [Required]
    public DateTime ReviewPeriodStart { get; set; }

    [Required]
    public DateTime ReviewPeriodEnd { get; set; }

    [Range(0, 5)]
    public decimal? OverallRating { get; set; }

    [MaxLength(1000)]
    public string? Strengths { get; set; }

    [MaxLength(1000)]
    public string? AreasForImprovement { get; set; }

    [MaxLength(1000)]
    public string? Comments { get; set; }

    [MaxLength(500)]
    public string? Goals { get; set; }
}

public class PerformanceReviewEditVM
{
    public int ReviewID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    public int? ReviewedByEmployeeID { get; set; }

    [Required]
    public DateTime ReviewDate { get; set; }

    [Required]
    public DateTime ReviewPeriodStart { get; set; }

    [Required]
    public DateTime ReviewPeriodEnd { get; set; }

    [Range(0, 5)]
    public decimal? OverallRating { get; set; }

    [MaxLength(1000)]
    public string? Strengths { get; set; }

    [MaxLength(1000)]
    public string? AreasForImprovement { get; set; }

    [MaxLength(1000)]
    public string? Comments { get; set; }

    [MaxLength(500)]
    public string? Goals { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region JobOpening

public class JobOpeningListVM
{
    public int JobOpeningID { get; set; }
    public string Title { get; set; } = "";
    public string? DepartmentName { get; set; }
    public string EmploymentType { get; set; } = "";
    public string? Location { get; set; }
    public int NumberOfPositions { get; set; }
    public string Status { get; set; } = "";
    public DateTime PostedDate { get; set; }
}

public class JobOpeningCreateVM
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public int DepartmentID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmploymentType { get; set; } = "";

    [MaxLength(100)]
    public string? Location { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalaryMin { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalaryMax { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int NumberOfPositions { get; set; }

    [MaxLength(1000)]
    public string? JobDescription { get; set; }

    [MaxLength(1000)]
    public string? Requirements { get; set; }

    [Required]
    public DateTime PostedDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [Required]
    public int CreatedByEmployeeID { get; set; }
}

public class JobOpeningEditVM
{
    public int JobOpeningID { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public int DepartmentID { get; set; }

    public int? BranchID { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmploymentType { get; set; } = "";

    [MaxLength(100)]
    public string? Location { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalaryMin { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalaryMax { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int NumberOfPositions { get; set; }

    [MaxLength(1000)]
    public string? JobDescription { get; set; }

    [MaxLength(1000)]
    public string? Requirements { get; set; }

    [Required]
    public DateTime PostedDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Applicant

public class ApplicantListVM
{
    public int ApplicantID { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobOpeningTitle { get; set; }
    public string Status { get; set; } = "";
    public DateTime AppliedDate { get; set; }
}

public class ApplicantCreateVM
{
    [Required]
    public int JobOpeningID { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = "";

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ResumePath { get; set; }

    [MaxLength(500)]
    public string? CoverLetter { get; set; }

    [MaxLength(50)]
    public string? LinkedInProfile { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Applied";

    [Required]
    public int ReferredByEmployeeID { get; set; }
}

public class ApplicantEditVM
{
    public int ApplicantID { get; set; }

    [Required]
    public int JobOpeningID { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = "";

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? ResumePath { get; set; }

    [MaxLength(500)]
    public string? CoverLetter { get; set; }

    [MaxLength(50)]
    public string? LinkedInProfile { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    public DateTime? InterviewDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime AppliedDate { get; set; }
}

#endregion

// ============================================================
// CUSTOMER SERVICE DEPARTMENT VIEW MODELS
// ============================================================

#region SupportTicket

public class SupportTicketListVM
{
    public int TicketID { get; set; }
    public string TicketNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? CustomerName { get; set; }
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SupportTicketCreateVM
{
    [Required]
    [MaxLength(20)]
    public string TicketNumber { get; set; } = "";

    [Required]
    public int CustomerID { get; set; }

    public int? AssignedToEmployeeID { get; set; }
    public int? BranchID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string TicketType { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class SupportTicketEditVM
{
    public int TicketID { get; set; }

    [Required]
    [MaxLength(20)]
    public string TicketNumber { get; set; } = "";

    [Required]
    public int CustomerID { get; set; }

    public int? AssignedToEmployeeID { get; set; }
    public int? BranchID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string TicketType { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(1000)]
    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

// ============================================================
// EXECUTIVE / SYSTEM VIEW MODELS
// ============================================================

#region Region

public class RegionListVM
{
    public int RegionID { get; set; }
    public string RegionCode { get; set; } = "";
    public string RegionName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class RegionCreateVM
{
    [Required]
    [MaxLength(20)]
    public string RegionCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string RegionName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class RegionEditVM
{
    public int RegionID { get; set; }

    [Required]
    [MaxLength(20)]
    public string RegionCode { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string RegionName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Branch

public class BranchListVM
{
    public int BranchID { get; set; }
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string? RegionName { get; set; }
    public string? City { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; }
}

public class BranchCreateVM
{
    [Required]
    [MaxLength(20)]
    public string BranchCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string BranchName { get; set; } = "";

    public int? RegionID { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public bool IsHeadOffice { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class BranchEditVM
{
    public int BranchID { get; set; }

    [Required]
    [MaxLength(20)]
    public string BranchCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string BranchName { get; set; } = "";

    public int? RegionID { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public bool IsHeadOffice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

#region Department

public class DepartmentListVM
{
    public int DepartmentID { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class DepartmentCreateVM
{
    [Required]
    [MaxLength(20)]
    public string DepartmentCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string DepartmentName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class DepartmentEditVM
{
    public int DepartmentID { get; set; }

    [Required]
    [MaxLength(20)]
    public string DepartmentCode { get; set; } = "";

    [Required]
    [MaxLength(150)]
    public string DepartmentName { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

#endregion

// ============================================================
// CRUD INDEX VIEW MODELS
// ============================================================

public class InventoryCrudIndexVM
{
    public int ProductCount { get; set; }
    public int CategoryCount { get; set; }
    public int WarehouseCount { get; set; }
    public int SupplierCount { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int PurchaseReceiptCount { get; set; }
    public int PurchaseInvoiceCount { get; set; }
    public int SupplierPaymentCount { get; set; }
}

public class ExcelImportModalVM
{
    public string ModalId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string EntityDisplayName { get; set; } = "";
}
