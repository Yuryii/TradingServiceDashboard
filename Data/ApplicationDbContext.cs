using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Dashboard.Models;

namespace Dashboard.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Region> Regions { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<CustomerGroup> CustomerGroups { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<SalesChannel> SalesChannels { get; set; }
    public DbSet<MarketingCampaign> MarketingCampaigns { get; set; }
    public DbSet<MarketingLead> MarketingLeads { get; set; }
    public DbSet<OpportunityStage> OpportunityStages { get; set; }
    public DbSet<Opportunity> Opportunities { get; set; }
    public DbSet<OpportunityStageHistory> OpportunityStageHistories { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
    public DbSet<SalesInvoice> SalesInvoices { get; set; }
    public DbSet<SalesInvoiceDetail> SalesInvoiceDetails { get; set; }
    public DbSet<SalesReturn> SalesReturns { get; set; }
    public DbSet<SalesReturnDetail> SalesReturnDetails { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
    public DbSet<PurchaseReceipt> PurchaseReceipts { get; set; }
    public DbSet<PurchaseReceiptDetail> PurchaseReceiptDetails { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventorySnapshot> InventorySnapshots { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<DimDate> DimDates { get; set; }
    public DbSet<KpiTarget> KpiTargets { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<PerformanceReview> PerformanceReviews { get; set; }
    public DbSet<Payroll> Payrolls { get; set; }
    public DbSet<JobOpening> JobOpenings { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceDetail> PurchaseInvoiceDetails { get; set; }
    public DbSet<MarketingSpendDaily> MarketingSpendDailies { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationConfig> NotificationConfigs { get; set; }
    public DbSet<AIChatSession> AIChatSessions { get; set; }
    public DbSet<AIChatMessage> AIChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Expenses)
                .WithOne(exp => exp.Employee)
                .HasForeignKey(exp => exp.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ExpensesApproved)
                .WithOne(exp => exp.ApprovedByEmployee)
                .HasForeignKey(exp => exp.ApprovedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.LeaveRequests)
                .WithOne(lr => lr.Employee)
                .HasForeignKey(lr => lr.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.LeaveRequestsApproved)
                .WithOne(lr => lr.ApprovedByEmployee)
                .HasForeignKey(lr => lr.ApprovedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PerformanceReviews)
                .WithOne(pr => pr.Employee)
                .HasForeignKey(pr => pr.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PerformanceReviewsGiven)
                .WithOne(pr => pr.ReviewedByEmployee)
                .HasForeignKey(pr => pr.ReviewedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PurchaseOrdersRequested)
                .WithOne(po => po.RequestedByEmployee)
                .HasForeignKey(po => po.RequestedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.PurchaseOrdersApproved)
                .WithOne(po => po.ApprovedByEmployee)
                .HasForeignKey(po => po.ApprovedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.StageHistoryChanges)
                .WithOne(h => h.ChangedByEmployee)
                .HasForeignKey(h => h.ChangedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(o => o.CustomerID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.Lead)
                .WithMany()
                .HasForeignKey(o => o.LeadID)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OpportunityStageHistory>(entity =>
        {
            entity.HasOne(h => h.FromStage)
                .WithMany(s => s.StageHistoriesFrom)
                .HasForeignKey(h => h.FromStageID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.ToStage)
                .WithMany(s => s.StageHistoriesTo)
                .HasForeignKey(h => h.ToStageID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasOne(so => so.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(so => so.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesInvoice>(entity =>
        {
            entity.HasOne(si => si.Customer)
                .WithMany(c => c.SalesInvoices)
                .HasForeignKey(si => si.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasIndex(i => new { i.ProductID, i.WarehouseID, i.BranchID }).IsUnique();
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasIndex(s => s.TransactionNumber).IsUnique();
        });

        modelBuilder.Entity<JobOpening>(entity =>
        {
            entity.HasOne(j => j.CreatedByEmployee)
                .WithMany(e => e.JobOpenings)
                .HasForeignKey(j => j.CreatedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.HasOne(a => a.JobOpening)
                .WithMany(j => j.Applicants)
                .HasForeignKey(a => a.JobOpeningID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ReferredByEmployee)
                .WithMany(e => e.Applicants)
                .HasForeignKey(a => a.ReferredByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIChatSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.Department);
            entity.HasIndex(s => s.LastMessageAt);
        });

        modelBuilder.Entity<AIChatMessage>(entity =>
        {
            entity.HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.SessionId);
            entity.HasIndex(m => m.CreatedAt);
        });
    }
}
