using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

public static class CrudServiceRegistry
{
    public static void RegisterAll(IServiceCollection services)
    {
        // Sales
        RegisterCrudService<Customer>(services, ctx => ctx.Customers);
        RegisterCrudService<CustomerGroup>(services, ctx => ctx.CustomerGroups);
        RegisterCrudService<SalesChannel>(services, ctx => ctx.SalesChannels);
        RegisterCrudService<OpportunityStage>(services, ctx => ctx.OpportunityStages);
        RegisterCrudService<Opportunity>(services, ctx => ctx.Opportunities);
        RegisterCrudService<OpportunityStageHistory>(services, ctx => ctx.OpportunityStageHistories);
        RegisterCrudService<Quote>(services, ctx => ctx.Quotes);
        RegisterCrudService<SalesOrder>(services, ctx => ctx.SalesOrders);
        RegisterCrudService<SalesOrderDetail>(services, ctx => ctx.SalesOrderDetails);
        RegisterCrudService<SalesInvoice>(services, ctx => ctx.SalesInvoices);
        RegisterCrudService<SalesInvoiceDetail>(services, ctx => ctx.SalesInvoiceDetails);
        RegisterCrudService<SalesReturn>(services, ctx => ctx.SalesReturns);
        RegisterCrudService<SalesReturnDetail>(services, ctx => ctx.SalesReturnDetails);
        RegisterCrudService<CustomerPayment>(services, ctx => ctx.CustomerPayments);

        // Marketing
        RegisterCrudService<MarketingCampaign>(services, ctx => ctx.MarketingCampaigns);
        RegisterCrudService<MarketingLead>(services, ctx => ctx.MarketingLeads);
        RegisterCrudService<MarketingSpendDaily>(services, ctx => ctx.MarketingSpendDailies);

        // Inventory
        RegisterCrudService<Product>(services, ctx => ctx.Products);
        RegisterCrudService<ProductCategory>(services, ctx => ctx.ProductCategories);
        RegisterCrudService<Warehouse>(services, ctx => ctx.Warehouses);
        RegisterCrudService<Supplier>(services, ctx => ctx.Suppliers);
        RegisterCrudService<PurchaseOrder>(services, ctx => ctx.PurchaseOrders);
        RegisterCrudService<PurchaseOrderDetail>(services, ctx => ctx.PurchaseOrderDetails);
        RegisterCrudService<PurchaseReceipt>(services, ctx => ctx.PurchaseReceipts);
        RegisterCrudService<PurchaseReceiptDetail>(services, ctx => ctx.PurchaseReceiptDetails);
        RegisterCrudService<PurchaseInvoice>(services, ctx => ctx.PurchaseInvoices);
        RegisterCrudService<PurchaseInvoiceDetail>(services, ctx => ctx.PurchaseInvoiceDetails);
        RegisterCrudService<SupplierPayment>(services, ctx => ctx.SupplierPayments);
        RegisterCrudService<Inventory>(services, ctx => ctx.Inventories);
        RegisterCrudService<InventorySnapshot>(services, ctx => ctx.InventorySnapshots);
        RegisterCrudService<StockTransaction>(services, ctx => ctx.StockTransactions);

        // Finance
        RegisterCrudService<Expense>(services, ctx => ctx.Expenses);
        RegisterCrudService<ExpenseCategory>(services, ctx => ctx.ExpenseCategories);

        // HR
        RegisterCrudService<Employee>(services, ctx => ctx.Employees);
        RegisterCrudService<Position>(services, ctx => ctx.Positions);
        RegisterCrudService<Attendance>(services, ctx => ctx.Attendances);
        RegisterCrudService<LeaveRequest>(services, ctx => ctx.LeaveRequests);
        RegisterCrudService<Payroll>(services, ctx => ctx.Payrolls);
        RegisterCrudService<PerformanceReview>(services, ctx => ctx.PerformanceReviews);
        RegisterCrudService<JobOpening>(services, ctx => ctx.JobOpenings);
        RegisterCrudService<Applicant>(services, ctx => ctx.Applicants);

        // CustomerService
        RegisterCrudService<SupportTicket>(services, ctx => ctx.SupportTickets);

        // Executive / System
        RegisterCrudService<Region>(services, ctx => ctx.Regions);
        RegisterCrudService<Branch>(services, ctx => ctx.Branches);
        RegisterCrudService<Department>(services, ctx => ctx.Departments);
        RegisterCrudService<DimDate>(services, ctx => ctx.DimDates);
        RegisterCrudService<KpiTarget>(services, ctx => ctx.KpiTargets);
    }

    private static void RegisterCrudService<TEntity>(IServiceCollection services, Func<ApplicationDbContext, DbSet<TEntity>> selector) where TEntity : class
    {
        var interfaceType = typeof(ICrudService<TEntity>);
        var implementationType = typeof(CrudService<TEntity>);
        var selectorFunc = new DbSetSelector<TEntity>(ctx => selector(ctx));

        services.AddScoped(interfaceType, sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            return Activator.CreateInstance(implementationType, context, selectorFunc)!;
        });
    }
}
