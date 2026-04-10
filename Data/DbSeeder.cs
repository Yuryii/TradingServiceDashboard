using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Dashboard.Data;
using Dashboard.Models;

namespace Dashboard.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbSeeder> _logger;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DbSeeder(ApplicationDbContext context, ILogger<DbSeeder> logger,
        RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        if (_context.Database.EnsureCreated())
        {
            _logger.LogInformation("Database created. Starting seed...");
        }

        _logger.LogInformation("Starting to seed all tables...");

        await SeedRegionsAsync();
        await SeedCustomerGroupsAsync();
        await SeedSalesChannelsAsync();
        await SeedExpenseCategoriesAsync();
        await SeedProductCategoriesAsync();
        await SeedOpportunityStagesAsync();

        await SeedBranchesAsync();
        await SeedDepartmentsAsync();
        await SeedPositionsAsync();
        await SeedSuppliersAsync();
        await SeedWarehousesAsync();

        await SeedEmployeesAsync();
        await SeedCustomersAsync();
        await SeedProductsAsync();
        await SeedMarketingCampaignsAsync();
        await SeedMarketingLeadsAsync();
        await SeedQuotesAsync();
        await SeedOpportunitiesAsync();
        await SeedSalesOrdersAsync();
        await SeedSalesOrderDetailsAsync();
        await SeedSalesInvoicesAsync();
        await SeedSalesInvoiceDetailsAsync();
        await SeedCustomerPaymentsAsync();
        await SeedPurchaseOrdersAsync();
        await SeedPurchaseOrderDetailsAsync();
        await SeedPurchaseReceiptsAsync();
        await SeedPurchaseReceiptDetailsAsync();
        await SeedInventoriesAsync();
        await SeedSalesReturnsAsync();
        await SeedSalesReturnDetailsAsync();
        await SeedSupplierPaymentsAsync();
        await SeedExpensesAsync();
        await SeedAttendancesAsync();
        await SeedLeaveRequestsAsync();
        await SeedPerformanceReviewsAsync();
        await SeedPayrollsAsync();
        await SeedJobOpeningsAsync();
        await SeedApplicantsAsync();
        await SeedSupportTicketsAsync();
        await SeedPurchaseInvoicesAsync();
        await SeedPurchaseInvoiceDetailsAsync();
        await SeedMarketingSpendDailiesAsync();
        await SeedInventorySnapshotsAsync();
        await SeedOpportunityStageHistoriesAsync();
        await SeedDimDatesAsync();
        await SeedKpiTargetsAsync();
        await SeedNotificationConfigsAsync();
        await SeedNotificationTriggerDataAsync();

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeding completed successfully!");
    }

    /// <summary>
    /// Seed dữ liệu gần thời điểm hiện tại để trigger notification jobs.
    /// Các bản ghi này có CreatedAt / UpdatedAt / ChangedAt gần DateTime.Now
    /// để thỏa điều kiện của notification (DelayHours, threshold, v.v.)
    /// </summary>
    private async Task SeedNotificationTriggerDataAsync()
    {
        if (await _context.Notifications.AnyAsync()) { _logger.LogInformation("Notification trigger data already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var products = await _context.Products.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var suppliers = await _context.Suppliers.ToListAsync();
        var warehouses = await _context.Warehouses.ToListAsync();
        var channels = await _context.SalesChannels.ToListAsync();
        var campaigns = await _context.MarketingCampaigns.ToListAsync();
        var stages = await _context.OpportunityStages.ToListAsync();
        var opportunities = await _context.Opportunities.ToListAsync();
        var departments = await _context.Departments.ToListAsync();
        var jobs = await _context.JobOpenings.ToListAsync();

        var now = DateTime.UtcNow;

        // ── SAL_NEW_ORDER: Đơn hàng mới trong 1 giờ ─────────────────────
        var recentOrders = new List<SalesOrder>();
        for (int i = 1; i <= 5; i++)
        {
            recentOrders.Add(new SalesOrder
            {
                OrderNumber = $"SO-NEW-{i:D3}",
                CustomerID = customers[i % customers.Count].CustomerID,
                BranchID = branches[i % branches.Count].BranchID,
                SalesChannelID = channels[i % channels.Count].SalesChannelID,
                SalesEmployeeID = employees[i % employees.Count].EmployeeID,
                OrderDate = now.AddMinutes(-10 * i),
                DeliveryDate = now.AddDays(5),
                SubTotal = 5000 + (i * 1000),
                TaxAmount = (5000 + (i * 1000)) * 0.1m,
                TotalAmount = (5000 + (i * 1000)) * 1.1m,
                PaymentStatus = "Pending",
                DeliveryStatus = "Pending",
                ShippingAddress = "123 Trigger Street",
                ShippingCity = "Ho Chi Minh City",
                ShippingProvince = "Ho Chi Minh",
                ShippingCountry = "Vietnam",
                CreatedAt = now.AddMinutes(-5 * i)
            });
        }
        _context.SalesOrders.AddRange(recentOrders);
        await _context.SaveChangesAsync();

        // ── SAL_LARGE_ORDER: Đơn hàng lớn (>100M) trong 24 giờ ─────────
        for (int i = 1; i <= 3; i++)
        {
            var subtotal = 100_000_000m + (i * 10_000_000m);
            _context.SalesOrders.Add(new SalesOrder
            {
                OrderNumber = $"SO-LRG-{i:D3}",
                CustomerID = customers[(i + 5) % customers.Count].CustomerID,
                BranchID = branches[i % branches.Count].BranchID,
                SalesChannelID = channels[i % channels.Count].SalesChannelID,
                SalesEmployeeID = employees[i % employees.Count].EmployeeID,
                OrderDate = now.AddHours(-i * 5),
                DeliveryDate = now.AddDays(7),
                SubTotal = subtotal,
                TaxAmount = subtotal * 0.1m,
                TotalAmount = subtotal * 1.1m,
                PaymentStatus = "Paid",
                DeliveryStatus = "Pending",
                ShippingAddress = "456 Enterprise Ave",
                ShippingCity = "Hanoi",
                ShippingProvince = "Hanoi",
                ShippingCountry = "Vietnam",
                CreatedAt = now.AddHours(-i * 3)
            });
        }
        await _context.SaveChangesAsync();

        // ── SAL_OPP_STAGE_CHANGED: Thay đổi stage gần đây ───────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.OpportunityStageHistories.Add(new OpportunityStageHistory
            {
                OpportunityID = opportunities[i % opportunities.Count].OpportunityID,
                FromStageID = stages[i % stages.Count].StageID,
                ToStageID = stages[(i + 1) % stages.Count].StageID,
                ChangedByEmployeeID = employees[i % employees.Count].EmployeeID,
                ChangedAt = now.AddMinutes(-30 * i),
                Note = $"Stage changed for notification trigger {i}"
            });
        }
        await _context.SaveChangesAsync();

        // ── SAL_NEW_CUSTOMER: Khách hàng mới trong 24 giờ ────────────────
        for (int i = 1; i <= 5; i++)
        {
            _context.Customers.Add(new Customer
            {
                CustomerCode = $"C-NEW-{i:D3}",
                CustomerName = $"New Customer {i} Trigger",
                CustomerType = "B2B",
                CustomerGroupID = customers[0].CustomerGroupID,
                RegionID = customers[0].RegionID,
                BranchID = branches[i % branches.Count].BranchID,
                Industry = "Technology",
                TaxCode = $"99{i:D9}",
                Phone = $"+84-90{i:D2}-111-222",
                Email = $"newcustomer{i}@email.com",
                AddressLine = $"{100 + i} New Customer Street",
                City = "Ho Chi Minh City",
                Province = "Ho Chi Minh",
                Country = "Vietnam",
                JoinDate = now.Date,
                CreditLimit = 50000,
                PaymentTermDays = 30,
                IsActive = true,
                CreatedAt = now.AddHours(-i * 2)
            });
        }
        await _context.SaveChangesAsync();

        // ── FIN_NEW_PAYMENT: Thanh toán mới từ khách hàng trong 24 giờ ──
        var orders = await _context.SalesOrders.ToListAsync();
        for (int i = 1; i <= 5; i++)
        {
            _context.CustomerPayments.Add(new CustomerPayment
            {
                PaymentNumber = $"CP-NEW-{i:D3}",
                CustomerID = customers[i % customers.Count].CustomerID,
                SalesOrderID = orders[i % orders.Count].SalesOrderID,
                ProcessedByEmployeeID = employees[i % employees.Count].EmployeeID,
                PaymentDate = now.AddHours(-i * 3),
                Amount = 5000 + (i * 1000),
                PaymentMethod = "Bank Transfer",
                ReferenceNumber = $"REF-NEW-{i:D6}",
                CreatedAt = now.AddHours(-i * 3)
            });
        }
        await _context.SaveChangesAsync();

        // ── FIN_OVERDUE_30D: Đơn hàng chưa thanh toán trên 30 ngày ──────
        for (int i = 1; i <= 5; i++)
        {
            _context.SalesOrders.Add(new SalesOrder
            {
                OrderNumber = $"SO-OVR-{i:D3}",
                CustomerID = customers[(i + 10) % customers.Count].CustomerID,
                BranchID = branches[i % branches.Count].BranchID,
                SalesChannelID = channels[i % channels.Count].SalesChannelID,
                SalesEmployeeID = employees[i % employees.Count].EmployeeID,
                OrderDate = now.AddDays(-35 - i * 5),
                DeliveryDate = now.AddDays(-25 - i * 5),
                SubTotal = 3000 + (i * 500),
                TaxAmount = (3000 + (i * 500)) * 0.1m,
                TotalAmount = (3000 + (i * 500)) * 1.1m,
                PaymentStatus = "Pending",
                DeliveryStatus = "Delivered",
                ShippingAddress = $"{200 + i} Overdue Street",
                ShippingCity = "Da Nang",
                ShippingProvince = "Da Nang",
                ShippingCountry = "Vietnam",
                CreatedAt = now.AddDays(-35 - i * 5)
            });
        }
        await _context.SaveChangesAsync();

        // ── FIN_EXPENSE_PENDING: Chi phí chờ phê duyệt ────────────────
        for (int i = 1; i <= 5; i++)
        {
            _context.Expenses.Add(new Expense
            {
                ExpenseNumber = $"EX-PND-{i:D3}",
                EmployeeID = employees[i % employees.Count].EmployeeID,
                BranchID = branches[i % branches.Count].BranchID,
                CategoryID = (await _context.ExpenseCategories.ToListAsync())[i % 10].ExpenseCategoryID,
                ExpenseDate = now.AddDays(-i),
                Amount = 500 + (i * 100),
                Description = $"Pending expense for notification trigger {i}",
                Status = "Pending",
                CreatedAt = now.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        // ── FIN_OVER_BUDGET: Chi phí vượt ngưỡng ────────────────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.Expenses.Add(new Expense
            {
                ExpenseNumber = $"EX-OVR-{i:D3}",
                EmployeeID = employees[(i + 5) % employees.Count].EmployeeID,
                BranchID = branches[(i + 1) % branches.Count].BranchID,
                CategoryID = (await _context.ExpenseCategories.ToListAsync())[(i + 3) % 10].ExpenseCategoryID,
                ExpenseDate = now.AddDays(-i),
                Amount = 10000 + (i * 5000),
                Description = $"Over budget expense for trigger {i}",
                Status = "Approved",
                ApprovedByEmployeeID = employees[(i + 1) % employees.Count].EmployeeID,
                CreatedAt = now.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        // ── FIN_LARGE_INVOICE: Hóa đơn chưa thanh toán lớn (>50M) ──────
        for (int i = 1; i <= 3; i++)
        {
            var subtotal = 60_000_000m + (i * 10_000_000m);
            _context.SalesInvoices.Add(new SalesInvoice
            {
                InvoiceNumber = $"INV-LRG-{i:D3}",
                SalesOrderID = orders[i % orders.Count].SalesOrderID,
                CustomerID = customers[(i + 8) % customers.Count].CustomerID,
                BranchID = branches[i % branches.Count].BranchID,
                SalesEmployeeID = employees[i % employees.Count].EmployeeID,
                InvoiceDate = now.AddDays(-10 - i),
                DueDate = now.AddDays(20),
                SubTotal = subtotal,
                TaxAmount = subtotal * 0.1m,
                TotalAmount = subtotal * 1.1m,
                PaidAmount = 0,
                OutstandingAmount = subtotal * 1.1m,
                PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-10 - i)
            });
        }
        await _context.SaveChangesAsync();

        // ── INV_LOW_STOCK: Tồn kho thấp (dưới reorder point) ────────────
        var inventories = await _context.Inventories.ToListAsync();
        for (int i = 0; i < Math.Min(5, inventories.Count); i++)
        {
            var inv = inventories[i];
            inv.QuantityAvailable = 5 + i;
            inv.QuantityOnHand = 15 + i;
            inv.LastUpdatedAt = now;
        }
        _context.Inventories.UpdateRange(inventories.Take(5));
        await _context.SaveChangesAsync();

        // ── INV_OUT_OF_STOCK: Hết hàng ───────────────────────────────────
        if (inventories.Count >= 10)
        {
            for (int i = 5; i < 8; i++)
            {
                var inv = inventories[i];
                inv.QuantityAvailable = 0;
                inv.QuantityOnHand = 0;
                inv.LastUpdatedAt = now;
            }
            _context.Inventories.UpdateRange(inventories.Skip(5).Take(3));
            await _context.SaveChangesAsync();
        }

        // ── INV_PO_PENDING: Purchase order chờ duyệt ────────────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.PurchaseOrders.Add(new PurchaseOrder
            {
                OrderNumber = $"PO-PND-{i:D3}",
                SupplierID = suppliers[i % suppliers.Count].SupplierID,
                BranchID = branches[i % branches.Count].BranchID,
                WarehouseID = warehouses[i % warehouses.Count].WarehouseID,
                RequestedByEmployeeID = employees[i % employees.Count].EmployeeID,
                OrderDate = now.AddDays(-2),
                ExpectedDeliveryDate = now.AddDays(12),
                SubTotal = 2000 + (i * 500),
                TaxAmount = (2000 + (i * 500)) * 0.1m,
                TotalAmount = (2000 + (i * 500)) * 1.1m,
                Status = "Submitted",
                CreatedAt = now.AddDays(-2)
            });
        }
        await _context.SaveChangesAsync();

        // ── INV_NEW_RECEIPT: Nhập kho gần đây ───────────────────────────
        var pos = await _context.PurchaseOrders.ToListAsync();
        for (int i = 1; i <= 3; i++)
        {
            _context.PurchaseReceipts.Add(new PurchaseReceipt
            {
                ReceiptNumber = $"PR-NEW-{i:D3}",
                PurchaseOrderID = pos[i % pos.Count].PurchaseOrderID,
                SupplierID = suppliers[i % suppliers.Count].SupplierID,
                WarehouseID = warehouses[i % warehouses.Count].WarehouseID,
                BranchID = branches[i % branches.Count].BranchID,
                ReceivedByEmployeeID = employees[i % employees.Count].EmployeeID,
                ReceiptDate = now.AddHours(-i * 5),
                TotalAmount = 3000 + (i * 500),
                Status = "Received",
                CreatedAt = now.AddHours(-i * 5)
            });
        }
        await _context.SaveChangesAsync();

        // ── HR_LEAVE_PENDING: Đơn nghỉ phép chờ duyệt ──────────────────
        for (int i = 1; i <= 5; i++)
        {
            _context.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeID = employees[i % employees.Count].EmployeeID,
                LeaveType = "Annual",
                StartDate = now.AddDays(i + 1),
                EndDate = now.AddDays(i + 3),
                TotalDays = 3,
                Reason = $"Leave request for notification trigger {i}",
                Status = "Pending",
                CreatedAt = now.AddHours(-i * 2)
            });
        }
        await _context.SaveChangesAsync();

        // ── HR_NEW_APPLICANT: Ứng viên mới gần đây ─────────────────────
        if (jobs.Count > 0)
        {
            for (int i = 1; i <= 5; i++)
            {
                _context.Applicants.Add(new Applicant
                {
                    JobOpeningID = jobs[i % jobs.Count].JobOpeningID,
                    FullName = $"New Applicant {i} Trigger",
                    Gender = i % 2 == 0 ? "Male" : "Female",
                    DateOfBirth = new DateTime(1995 + (i % 10), (i % 12) + 1, (i % 28) + 1),
                    Phone = $"+84-90{i:D2}-333-444",
                    Email = $"newapplicant{i}@email.com",
                    Address = $"{300 + i} Applicant Street",
                    Status = "Applied",
                    AppliedDate = now.AddHours(-i * 4),
                    ReferredByEmployeeID = employees[i % employees.Count].EmployeeID,
                    CreatedAt = now.AddHours(-i * 4)
                });
            }
            await _context.SaveChangesAsync();
        }

        // ── HR_NEW_EMPLOYEE: Nhân viên mới trong 7 ngày ─────────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.Employees.Add(new Employee
            {
                EmployeeCode = $"EMP-NEW-{i:D3}",
                FullName = $"New Employee {i} Trigger",
                Gender = i % 2 == 0 ? "Male" : "Female",
                DateOfBirth = new DateTime(1990 + (i % 10), (i % 12) + 1, (i % 28) + 1),
                Phone = $"+84-90{i:D2}-555-666",
                Email = $"newemployee{i}@company.com",
                AddressLine = $"{400 + i} New Employee Street",
                DepartmentID = departments[i % departments.Count].DepartmentID,
                PositionID = (await _context.Positions.ToListAsync())[i % 10].PositionID,
                BranchID = branches[i % branches.Count].BranchID,
                EmploymentType = "Probation",
                HireDate = now.AddDays(-i * 2),
                BaseSalary = 8000 + (i * 500),
                IsActive = true,
                CreatedAt = now.AddDays(-i * 2)
            });
        }
        await _context.SaveChangesAsync();

        // ── CS_NEW_TICKET: Ticket mới trong 1 giờ ───────────────────────
        for (int i = 1; i <= 5; i++)
        {
            _context.SupportTickets.Add(new SupportTicket
            {
                TicketNumber = $"TK-NEW-{i:D3}",
                CustomerID = customers[i % customers.Count].CustomerID,
                AssignedToEmployeeID = employees[i % employees.Count].EmployeeID,
                BranchID = branches[i % branches.Count].BranchID,
                Subject = $"New support ticket {i} for notification trigger",
                TicketType = "Technical",
                Priority = i % 3 == 0 ? "Critical" : (i % 2 == 0 ? "High" : "Medium"),
                Status = "Open",
                Description = $"Support ticket description {i}",
                CreatedAt = now.AddMinutes(-20 * i)
            });
        }
        await _context.SaveChangesAsync();

        // ── CS_HIGH_PRIORITY: Ticket ưu tiên cao chưa xử lý ─────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.SupportTickets.Add(new SupportTicket
            {
                TicketNumber = $"TK-HP-{i:D3}",
                CustomerID = customers[(i + 5) % customers.Count].CustomerID,
                AssignedToEmployeeID = employees[i % employees.Count].EmployeeID,
                BranchID = branches[(i + 1) % branches.Count].BranchID,
                Subject = $"High priority ticket {i} for trigger",
                TicketType = "Billing",
                Priority = i % 2 == 0 ? "Critical" : "High",
                Status = "Open",
                Description = $"High priority description {i}",
                CreatedAt = now.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        // ── CS_TICKET_SLA_BREACH: Ticket quá hạn >24 giờ ────────────────
        for (int i = 1; i <= 3; i++)
        {
            _context.SupportTickets.Add(new SupportTicket
            {
                TicketNumber = $"TK-SLA-{i:D3}",
                CustomerID = customers[(i + 8) % customers.Count].CustomerID,
                AssignedToEmployeeID = employees[(i + 2) % employees.Count].EmployeeID,
                BranchID = branches[(i + 2) % branches.Count].BranchID,
                Subject = $"SLA breach ticket {i} for trigger",
                TicketType = "General",
                Priority = "Medium",
                Status = "In Progress",
                Description = $"SLA breach description {i}",
                CreatedAt = now.AddHours(-30 - i * 5),
                ResolvedDate = null
            });
        }
        await _context.SaveChangesAsync();

        // ── MKT_BUDGET_80: Campaign ngân sách gần hết (>80%) ───────────
        if (campaigns.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                var c = campaigns[i];
                c.ActualSpend = (decimal)(c.Budget * 0.85m);
                c.IsActive = true;
                c.Status = "Active";
            }
            _context.MarketingCampaigns.UpdateRange(campaigns.Take(3));
            await _context.SaveChangesAsync();
        }

        // ── MKT_BUDGET_EXCEEDED: Campaign vượt ngân sách ────────────────
        if (campaigns.Count >= 5)
        {
            for (int i = 3; i < 5; i++)
            {
                var c = campaigns[i];
                c.ActualSpend = c.Budget + 5000;
                c.IsActive = true;
                c.Status = "Active";
            }
            _context.MarketingCampaigns.UpdateRange(campaigns.Skip(3).Take(2));
            await _context.SaveChangesAsync();
        }

        // ── MKT_NEW_LEAD: Lead mới trong 24 giờ ─────────────────────────
        for (int i = 1; i <= 5; i++)
        {
            _context.MarketingLeads.Add(new MarketingLead
            {
                LeadCode = $"LD-NEW-{i:D3}",
                LeadName = $"New Lead {i} Trigger",
                CompanyName = $"New Lead Company {i}",
                CampaignID = campaigns[i % campaigns.Count].CampaignID,
                AssignedEmployeeID = employees[i % employees.Count].EmployeeID,
                Status = "New",
                Source = "Website",
                Email = $"newlead{i}@email.com",
                Phone = $"+84-90{i:D2}-777-888",
                LeadScore = 50 + (i * 10),
                CreatedDate = now.AddHours(-i * 4)
            });
        }
        await _context.SaveChangesAsync();

        // ── MKT_LEAD_CONVERTED: Lead được chuyển đổi gần đây ───────────
        for (int i = 1; i <= 3; i++)
        {
            _context.MarketingLeads.Add(new MarketingLead
            {
                LeadCode = $"LD-CVT-{i:D3}",
                LeadName = $"Converted Lead {i} Trigger",
                CompanyName = $"Converted Company {i}",
                CampaignID = campaigns[(i + 2) % campaigns.Count].CampaignID,
                AssignedEmployeeID = employees[(i + 1) % employees.Count].EmployeeID,
                Status = "Converted",
                ConvertedDate = now.AddHours(-i * 6),
                Source = "Referral",
                Email = $"convertedlead{i}@email.com",
                Phone = $"+84-90{i:D2}-999-000",
                LeadScore = 90 + i,
                CreatedDate = now.AddDays(-5)
            });
        }
        await _context.SaveChangesAsync();

        // ── MKT_LOW_ROAS: Chiến dịch ROAS thấp (hôm qua) ──────────────
        var yesterday = DateTime.Today.AddDays(-1);
        var spendDailies = await _context.MarketingSpendDailies.ToListAsync();
        for (int i = 0; i < Math.Min(3, spendDailies.Count); i++)
        {
            spendDailies[i].SpendDate = yesterday;
            spendDailies[i].CPA = 5.0m + (i * 2);
        }
        _context.MarketingSpendDailies.UpdateRange(spendDailies.Take(3));
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification trigger data seeded successfully.");
    }

    /// <summary>
    /// Seed NotificationConfigs nếu chưa có. Đây là 37 notification jobs mặc định.
    /// </summary>
    private async Task SeedNotificationConfigsAsync()
    {
        if (await _context.NotificationConfigs.AnyAsync())
        {
            _logger.LogInformation("NotificationConfigs already seeded. Skipping.");
            return;
        }

        var configs = NotificationConfigDefaults.DefaultConfigs;
        foreach (var config in configs)
        {
            config.CreatedAt = DateTime.UtcNow;
        }
        _context.NotificationConfigs.AddRange(configs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} notification configurations.", configs.Count);
    }

    private async Task SeedRegionsAsync()
    {
        if (await _context.Regions.AnyAsync()) { _logger.LogInformation("Regions already seeded. Skipping."); return; }
        var regions = new List<Region>();
        var names = new[] { "North", "South", "Central", "East", "West", "Northeast", "Northwest", "Southeast", "Southwest", "Metro" };
        for (int i = 1; i <= 10; i++)
        {
            regions.Add(new Region { RegionCode = $"R{i:D3}", RegionName = $"{names[i-1]} Region", Description = $"Covers {names[i-1].ToLower()} area", IsActive = true });
        }
        _context.Regions.AddRange(regions);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCustomerGroupsAsync()
    {
        if (await _context.CustomerGroups.AnyAsync()) { _logger.LogInformation("CustomerGroups already seeded. Skipping."); return; }
        var groups = new List<CustomerGroup>();
        var names = new[] { "Premium", "Standard", "Basic", "VIP", "Enterprise", "SME", "Corporate", "Government", "Retail", "Wholesale" };
        for (int i = 1; i <= 10; i++)
        {
            groups.Add(new CustomerGroup { GroupCode = $"CG{i:D3}", GroupName = $"{names[i-1]} Group", Description = $"Customer group for {names[i-1].ToLower()}", IsActive = true });
        }
        _context.CustomerGroups.AddRange(groups);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesChannelsAsync()
    {
        if (await _context.SalesChannels.AnyAsync()) { _logger.LogInformation("SalesChannels already seeded. Skipping."); return; }
        var channels = new List<SalesChannel>();
        var names = new[] { "Online", "Retail", "Wholesale", "Direct", "Partner", "Distributor", "Reseller", "E-commerce", "TeleSales", "FieldSales" };
        for (int i = 1; i <= 10; i++)
        {
            channels.Add(new SalesChannel { ChannelCode = $"SC{i:D3}", ChannelName = $"{names[i-1]}", Description = $"Sales channel for {names[i-1].ToLower()}", IsActive = true });
        }
        _context.SalesChannels.AddRange(channels);
        await _context.SaveChangesAsync();
    }

    private async Task SeedExpenseCategoriesAsync()
    {
        if (await _context.ExpenseCategories.AnyAsync()) { _logger.LogInformation("ExpenseCategories already seeded. Skipping."); return; }
        var categories = new List<ExpenseCategory>();
        var names = new[] { "Travel", "Meals", "Equipment", "Software", "Utilities", "Marketing", "Training", "Office Supplies", "Communication", "Maintenance" };
        for (int i = 1; i <= 10; i++)
        {
            categories.Add(new ExpenseCategory { CategoryCode = $"EC{i:D3}", CategoryName = $"{names[i-1]}", Description = $"Expense category for {names[i-1].ToLower()}", IsActive = true });
        }
        _context.ExpenseCategories.AddRange(categories);
        await _context.SaveChangesAsync();
    }

    private async Task SeedProductCategoriesAsync()
    {
        if (await _context.ProductCategories.AnyAsync()) { _logger.LogInformation("ProductCategories already seeded. Skipping."); return; }
        var categories = new List<ProductCategory>();
        var names = new[] { "Electronics", "Computers", "Phones", "Furniture", "Office", "Software", "Gaming", "Audio", "Accessories", "Tools" };
        for (int i = 1; i <= 10; i++)
        {
            categories.Add(new ProductCategory { CategoryCode = $"PC{i:D3}", CategoryName = $"{names[i-1]}", ParentCategoryID = i > 5 ? (i % 5) + 1 : null, Description = $"Product category for {names[i-1].ToLower()}", IsActive = true });
        }
        _context.ProductCategories.AddRange(categories);
        await _context.SaveChangesAsync();
    }

    private async Task SeedOpportunityStagesAsync()
    {
        if (await _context.OpportunityStages.AnyAsync()) { _logger.LogInformation("OpportunityStages already seeded. Skipping."); return; }
        var stages = new List<OpportunityStage>();
        var names = new[] { "New", "Contacted", "Qualified", "Proposal", "Negotiation", "Closed Won", "Closed Lost", "On Hold", "Follow Up", "Converted" };
        for (int i = 1; i <= 10; i++)
        {
            stages.Add(new OpportunityStage { StageCode = $"STG{i:D3}", StageName = names[i-1], StageOrder = i, IsWonStage = i == 6, IsLostStage = i == 7, IsClosedStage = i >= 6 });
        }
        _context.OpportunityStages.AddRange(stages);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBranchesAsync()
    {
        if (await _context.Branches.AnyAsync()) { _logger.LogInformation("Branches already seeded. Skipping."); return; }
        var regions = await _context.Regions.ToListAsync();
        var cities = new[] { "Ho Chi Minh City", "Hanoi", "Da Nang", "Can Tho", "Hai Phong", "Nha Trang", "Hue", "Vung Tau", "Bien Hoa", "Cao Lanh" };
        var branches = new List<Branch>();
        for (int i = 1; i <= 10; i++)
        {
            branches.Add(new Branch
            {
                BranchCode = $"B{i:D3}",
                BranchName = $"{cities[i-1]} Branch",
                RegionID = regions[(i-1) % regions.Count].RegionID,
                AddressLine = $"{100 + i} Main Street",
                City = cities[i-1],
                Province = cities[i-1],
                Country = "Vietnam",
                Phone = $"+84-{i:D3}-XXX-XXXX",
                Email = $"branch{i}@company.com",
                IsHeadOffice = i == 1,
                IsActive = true
            });
        }
        _context.Branches.AddRange(branches);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _context.Departments.AnyAsync()) { _logger.LogInformation("Departments already seeded. Skipping."); return; }
        var departments = new List<Department>();
        var names = new[] { "Sales", "Marketing", "Finance", "HR", "IT", "Operations", "Procurement", "Legal", "Admin", "Support" };
        for (int i = 1; i <= 10; i++)
        {
            departments.Add(new Department { DepartmentCode = $"D{i:D3}", DepartmentName = names[i-1], Description = $"Department of {names[i-1].ToLower()}", IsActive = true });
        }
        _context.Departments.AddRange(departments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPositionsAsync()
    {
        if (await _context.Positions.AnyAsync()) { _logger.LogInformation("Positions already seeded. Skipping."); return; }
        var positions = new List<Position>();
        var levels = new[] { "Staff", "Senior", "Supervisor", "Manager", "Director", "Executive" };
        var names = new[] { "Analyst", "Specialist", "Coordinator", "Administrator", "Assistant", "Lead", "Engineer", "Consultant", "Director", "Executive" };
        for (int i = 1; i <= 10; i++)
        {
            positions.Add(new Position
            {
                PositionCode = $"P{i:D3}",
                PositionName = $"{levels[i%levels.Length]} {names[i-1]}",
                PositionLevel = levels[i%levels.Length],
                Description = $"Position for {names[i-1].ToLower()}",
                IsActive = true
            });
        }
        _context.Positions.AddRange(positions);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSuppliersAsync()
    {
        if (await _context.Suppliers.AnyAsync()) { _logger.LogInformation("Suppliers already seeded. Skipping."); return; }
        var regions = await _context.Regions.ToListAsync();
        var suppliers = new List<Supplier>();
        var types = new[] { "Goods", "Services", "Mixed" };
        var names = new[] { "Global Trading", "Import Export", "Wholesale Co", "Distribution Inc", "Supply Chain", "Logistics Partners", "Manufacturing Ltd", "Industrial Supplies", "Commercial Products", "Quality Goods" };
        for (int i = 1; i <= 10; i++)
        {
            suppliers.Add(new Supplier
            {
                SupplierCode = $"S{i:D3}",
                SupplierName = names[i-1],
                SupplierType = types[i % types.Length],
                RegionID = regions[(i-1) % regions.Count].RegionID,
                TaxCode = $"0{i:D10}",
                Phone = $"+84-{i:D3}-XXX-XXXX",
                Email = $"supplier{i}@vendor.com",
                AddressLine = $"{200 + i} Supplier Street",
                City = "Ho Chi Minh City",
                Province = "Ho Chi Minh",
                Country = "Vietnam",
                PaymentTermDays = 30 + (i % 4) * 15,
                IsActive = true
            });
        }
        _context.Suppliers.AddRange(suppliers);
        await _context.SaveChangesAsync();
    }

    private async Task SeedWarehousesAsync()
    {
        if (await _context.Warehouses.AnyAsync()) { _logger.LogInformation("Warehouses already seeded. Skipping."); return; }
        var branches = await _context.Branches.ToListAsync();
        var warehouses = new List<Warehouse>();
        for (int i = 1; i <= 10; i++)
        {
            warehouses.Add(new Warehouse
            {
                WarehouseCode = $"W{i:D3}",
                WarehouseName = $"Warehouse {i}",
                BranchID = branches[(i-1) % branches.Count].BranchID,
                AddressLine = $"{300 + i} Warehouse Road",
                City = "Ho Chi Minh City",
                Province = "Ho Chi Minh",
                IsActive = true
            });
        }
        _context.Warehouses.AddRange(warehouses);
        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeesAsync()
    {
        if (await _context.Employees.AnyAsync()) { _logger.LogInformation("Employees already seeded. Skipping."); return; }
        var departments = await _context.Departments.ToListAsync();
        var positions = await _context.Positions.ToListAsync();
        var branches = await _context.Branches.ToListAsync();

        var employees = new List<Employee>();
        employees.Add(new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Nguyen Van A",
            Gender = "Male",
            DateOfBirth = new DateTime(1985, 1, 1),
            Phone = "+84-901-XXX-XXXX",
            Email = "nguyenvana@company.com",
            AddressLine = "123 Main Street, District 1",
            DepartmentID = departments[0].DepartmentID,
            PositionID = positions[0].PositionID,
            BranchID = branches[0].BranchID,
            ManagerID = null,
            EmploymentType = "FullTime",
            HireDate = new DateTime(2020, 1, 1),
            BaseSalary = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        var firstNames = new[] { "Tran Van", "Le Thi", "Pham Van", "Hoang Thi", "Vu Van", "Do Thi", "Ngo Van", "Bui Thi" };
        var lastNames = new[] { "An", "Binh", "Cuc", "Dung", "Em", "Phuc", "Quang", "Sang" };

        for (int i = 2; i <= 30; i++)
        {
            employees.Add(new Employee
            {
                EmployeeCode = $"EMP{i:D3}",
                FullName = $"{firstNames[(i-1)%firstNames.Length]} {lastNames[(i-1)%lastNames.Length]}",
                Gender = i % 2 == 0 ? "Male" : "Female",
                DateOfBirth = new DateTime(1980 + (i % 20), (i % 12) + 1, (i % 28) + 1),
                Phone = $"+84-90{i%100:D2}-XXX-XXXX",
                Email = $"employee{i}@company.com",
                AddressLine = $"{100+i} Employee Street",
                DepartmentID = departments[i % departments.Count].DepartmentID,
                PositionID = positions[i % positions.Count].PositionID,
                BranchID = branches[i % branches.Count].BranchID,
                ManagerID = i <= 10 ? 1 : (i % 10) + 1,
                EmploymentType = i % 5 == 0 ? "Contract" : "FullTime",
                HireDate = new DateTime(2022 + (i % 3), (i % 12) + 1, (i % 28) + 1),
                BaseSalary = 3000 + (i * 100),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCustomersAsync()
    {
        if (await _context.Customers.AnyAsync()) { _logger.LogInformation("Customers already seeded. Skipping."); return; }
        var customerGroups = await _context.CustomerGroups.ToListAsync();
        var regions = await _context.Regions.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var industries = new[] { "Technology", "Finance", "Healthcare", "Education", "Retail", "Manufacturing", "Construction", "Transportation", "Energy", "Telecommunications" };
        var companies = new[] { "ABC Corp", "XYZ Company", "Global Trading", "Intl Business", "Enterprise Sol", "Premium Svcs", "Quality Products", "Reliable Partners", "Trusted Co", "Leading Brand" };
        var cities = new[] { "Ho Chi Minh City", "Hanoi", "Da Nang", "Can Tho", "Hai Phong", "Nha Trang", "Hue", "Vung Tau", "Bien Hoa", "Cao Lanh" };

        var customers = new List<Customer>();
        for (int i = 1; i <= 30; i++)
        {
            customers.Add(new Customer
            {
                CustomerCode = $"C{i:D3}",
                CustomerName = companies[(i-1)%companies.Length],
                CustomerType = i % 2 == 0 ? "B2B" : "B2C",
                CustomerGroupID = customerGroups[(i-1)%customerGroups.Count].CustomerGroupID,
                RegionID = regions[(i-1)%regions.Count].RegionID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                Industry = industries[(i-1)%industries.Length],
                TaxCode = $"0{i:D9}",
                Phone = $"+84-90{i%100:D2}-XXX-XXXX",
                Email = $"customer{i}@email.com",
                AddressLine = $"{100+i} Customer Street",
                City = cities[(i-1)%cities.Length],
                Province = cities[(i-1)%cities.Length],
                Country = "Vietnam",
                JoinDate = new DateTime(2020+(i%5), (i%12)+1, (i%28)+1),
                CreditLimit = 10000+(i*1000),
                PaymentTermDays = 30+(i%4)*15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Customers.AddRange(customers);
        await _context.SaveChangesAsync();
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync()) { _logger.LogInformation("Products already seeded. Skipping."); return; }
        var categories = await _context.ProductCategories.ToListAsync();
        var products = new List<Product>();
        var types = new[] { "Stock", "Service", "Bundle" };
        var brands = new[] { "Brand A", "Brand B", "Brand C", "Brand D", "Brand E", "Brand F", "Brand G", "Brand H", "Brand I", "Brand J" };
        var names = new[] { "Widget A", "Widget B", "Gadget X", "Gadget Y", "Tool Pro", "Tool Plus", "Device Max", "Device Mini", "System Std", "System Pro" };
        var units = new[] { "Unit", "Pack", "Box", "Hour" };

        for (int i = 1; i <= 30; i++)
        {
            products.Add(new Product
            {
                ProductCode = $"PRD{i:D3}",
                ProductName = names[(i-1)%names.Length],
                CategoryID = categories[(i-1)%categories.Count].CategoryID,
                ProductType = types[i%types.Length],
                UnitOfMeasure = units[i%units.Length],
                Brand = brands[(i-1)%brands.Length],
                SalePrice = 10+(i*5),
                CostPrice = 5+(i*2),
                ReorderLevel = 10+(i%20),
                MaxStockLevel = 100+(i*10),
                IsStockItem = i%4 != 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMarketingCampaignsAsync()
    {
        if (await _context.MarketingCampaigns.AnyAsync()) { _logger.LogInformation("MarketingCampaigns already seeded. Skipping."); return; }
        var channels = new[] { "Email", "Social Media", "Print", "TV", "Radio", "Outdoor", "Event", "Webinar", "Content", "PPC" };
        var statuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled" };
        var names = new[] { "Spring Sale", "Summer Promo", "Winter Discount", "Fall Collection", "New Year Offer", "Holiday Special", "Black Friday", "Cyber Monday", "Flash Sale", "Clearance" };

        var campaigns = new List<MarketingCampaign>();
        for (int i = 1; i <= 10; i++)
        {
            campaigns.Add(new MarketingCampaign
            {
                CampaignCode = $"MC{i:D3}",
                CampaignName = names[(i-1)%names.Length],
                Channel = channels[(i-1)%channels.Length],
                StartDate = new DateTime(2026, 1, 1).AddDays(i),
                EndDate = new DateTime(2026, 1, 1).AddDays(i+30),
                Budget = 5000+(i*500),
                ActualSpend = 1000+(i*100),
                Objective = $"Marketing campaign for {names[(i-1)%names.Length].ToLower()}",
                Status = statuses[(i-1)%statuses.Length],
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.MarketingCampaigns.AddRange(campaigns);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMarketingLeadsAsync()
    {
        if (await _context.MarketingLeads.AnyAsync()) { _logger.LogInformation("MarketingLeads already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var campaigns = await _context.MarketingCampaigns.ToListAsync();
        var statuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
        var companies = new[] { "Tech Sol", "Enterprise", "SME", "Startup", "Corporate", "Govt", "Education", "Healthcare", "Retail", "Mfg" };

        var leads = new List<MarketingLead>();
        for (int i = 1; i <= 60; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 12 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var day = (i - 1) % 28 + 1;
            leads.Add(new MarketingLead
            {
                LeadCode = $"LD{i:D4}",
                LeadName = companies[(i - 1) % companies.Length],
                CompanyName = $"{companies[(i - 1) % companies.Length]} Inc",
                CampaignID = campaigns[(i - 1) % campaigns.Count].CampaignID,
                AssignedEmployeeID = employees[(i - 1) % employees.Count].EmployeeID,
                Status = statuses[(i - 1) % statuses.Length],
                Source = "Website",
                Email = $"lead{i}@email.com",
                Phone = $"+84-90{i % 100:D2}-XXX-XXXX",
                LeadScore = (i % 100) + 1,
                CreatedDate = new DateTime(year, month, day)
            });
        }
        _context.MarketingLeads.AddRange(leads);
        await _context.SaveChangesAsync();
    }

    private async Task SeedQuotesAsync()
    {
        if (await _context.Quotes.AnyAsync()) { _logger.LogInformation("Quotes already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var statuses = new[] { "Draft", "Sent", "Accepted", "Rejected", "Expired" };

        var quotes = new List<Quote>();
        for (int i = 1; i <= 20; i++)
        {
            var quoteDate = new DateTime(2026, 1, 1).AddDays(-30 + i);
            var subtotal = 1000+(i*100);
            quotes.Add(new Quote
            {
                QuoteNumber = $"QT{i:D3}",
                CustomerID = customers[(i-1)%customers.Count].CustomerID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                SalesEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                QuoteDate = quoteDate,
                ValidUntilDate = quoteDate.AddDays(30),
                SubTotal = subtotal,
                TaxAmount = subtotal*0.1m,
                TotalAmount = subtotal*1.1m,
                Status = statuses[(i-1)%statuses.Length],
                TermsAndConditions = $"Quote terms for {i}",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Quotes.AddRange(quotes);
        await _context.SaveChangesAsync();
    }

    private async Task SeedOpportunitiesAsync()
    {
        if (await _context.Opportunities.AnyAsync()) { _logger.LogInformation("Opportunities already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var leads = await _context.MarketingLeads.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var stages = await _context.OpportunityStages.ToListAsync();

        var opportunities = new List<Opportunity>();
        for (int i = 1; i <= 48; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 12 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var day = (i - 1) % 28 + 1;
            var closeDate = new DateTime(year, month, day);
            var isWon = i % 3 == 0;
            opportunities.Add(new Opportunity
            {
                OpportunityCode = $"OP{i:D4}",
                CustomerID = customers[(i - 1) % customers.Count].CustomerID,
                LeadID = leads[(i - 1) % leads.Count].LeadID,
                OwnerEmployeeID = employees[(i - 1) % employees.Count].EmployeeID,
                BranchID = branches[(i - 1) % branches.Count].BranchID,
                StageID = stages[(i - 1) % stages.Count].StageID,
                SourceChannel = "Direct",
                Probability = (i % 10 + 1) * 10,
                EstimatedValue = 5000 + (i * 500),
                ExpectedCloseDate = closeDate.AddMonths(1),
                ActualCloseDate = isWon ? closeDate : null,
                Status = isWon ? "Won" : (i % 5 == 0 ? "Lost" : (i % 2 == 0 ? "In Progress" : "Open")),
                CreatedAt = closeDate.AddDays(-7)
            });
        }
        _context.Opportunities.AddRange(opportunities);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesOrdersAsync()
    {
        if (await _context.SalesOrders.AnyAsync()) { _logger.LogInformation("SalesOrders already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var channels = await _context.SalesChannels.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var opportunities = await _context.Opportunities.ToListAsync();
        var quotes = await _context.Quotes.ToListAsync();
        var paymentStatuses = new[] { "Pending", "Partial", "Paid" };
        var deliveryStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        var orders = new List<SalesOrder>();
        for (int i = 1; i <= 60; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 12 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var day = (i - 1) % 28 + 1;
            var orderDate = new DateTime(year, month, day);
            var subtotal = 2000 + (i * 200);
            orders.Add(new SalesOrder
            {
                OrderNumber = $"SO{i:D4}",
                OpportunityID = opportunities[(i - 1) % opportunities.Count].OpportunityID,
                QuoteID = quotes[(i - 1) % quotes.Count].QuoteID,
                CustomerID = customers[(i - 1) % customers.Count].CustomerID,
                BranchID = branches[(i - 1) % branches.Count].BranchID,
                SalesChannelID = channels[(i - 1) % channels.Count].SalesChannelID,
                SalesEmployeeID = employees[(i - 1) % employees.Count].EmployeeID,
                OrderDate = orderDate,
                DeliveryDate = orderDate.AddDays(7),
                SubTotal = subtotal,
                TaxAmount = subtotal * 0.1m,
                DiscountAmount = i % 4 == 0 ? 100 : 0,
                TotalAmount = subtotal * 1.1m - (i % 4 == 0 ? 100 : 0),
                PaidAmount = i > 40 ? subtotal * 1.1m - (i % 4 == 0 ? 100 : 0) : 0,
                PaymentStatus = paymentStatuses[(i - 1) % paymentStatuses.Length],
                DeliveryStatus = deliveryStatuses[(i - 1) % deliveryStatuses.Length],
                ShippingAddress = $"{100 + i} Order Street",
                ShippingCity = "Ho Chi Minh City",
                ShippingProvince = "Ho Chi Minh",
                ShippingCountry = "Vietnam",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.SalesOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesOrderDetailsAsync()
    {
        if (await _context.SalesOrderDetails.AnyAsync()) { _logger.LogInformation("SalesOrderDetails already seeded. Skipping."); return; }
        var orders = await _context.SalesOrders.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<SalesOrderDetail>();
        for (int i = 1; i <= 120; i++)
        {
            var quantity = 1+(i%10);
            var unitPrice = 50+(i*10);
            var lineTotal = quantity*unitPrice;
            details.Add(new SalesOrderDetail
            {
                SalesOrderID = orders[(i-1)%orders.Count].SalesOrderID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountPercent = i%5==0 ? 10 : 0,
                LineTotal = lineTotal-(lineTotal*(i%5==0?0.1m:0)),
                QuantityShipped = i>15 ? quantity : 0
            });
        }
        _context.SalesOrderDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesInvoicesAsync()
    {
        if (await _context.SalesInvoices.AnyAsync()) { _logger.LogInformation("SalesInvoices already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var orders = await _context.SalesOrders.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Draft", "Sent", "Paid", "Overdue", "Cancelled" };

        var invoices = new List<SalesInvoice>();
        for (int i = 1; i <= 20; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 6 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var invoiceDate = new DateTime(year, month, 1).AddDays(i - 1);
            var subtotal = 1800+(i*180);
            var total = subtotal*1.1m-(i%4==0?90:0);
            invoices.Add(new SalesInvoice
            {
                InvoiceNumber = $"INV{i:D3}",
                SalesOrderID = orders[(i-1)%orders.Count].SalesOrderID,
                CustomerID = customers[(i-1)%customers.Count].CustomerID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                SalesEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                InvoiceDate = invoiceDate,
                DueDate = invoiceDate.AddDays(30),
                SubTotal = subtotal,
                TaxAmount = subtotal*0.1m,
                DiscountAmount = i%4==0 ? 90 : 0,
                TotalAmount = total,
                PaidAmount = i>15 ? total : 0,
                OutstandingAmount = i>15 ? 0 : total,
                PaymentStatus = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.SalesInvoices.AddRange(invoices);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesInvoiceDetailsAsync()
    {
        if (await _context.SalesInvoiceDetails.AnyAsync()) { _logger.LogInformation("SalesInvoiceDetails already seeded. Skipping."); return; }
        var invoices = await _context.SalesInvoices.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<SalesInvoiceDetail>();
        for (int i = 1; i <= 20; i++)
        {
            var quantity = 2+(i%8);
            var unitPrice = 60+(i*12);
            var lineTotal = quantity*unitPrice;
            details.Add(new SalesInvoiceDetail
            {
                InvoiceID = invoices[(i-1)%invoices.Count].InvoiceID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountPercent = i%5==0 ? 8 : 0,
                LineTotal = lineTotal-(lineTotal*(i%5==0?0.08m:0))
            });
        }
        _context.SalesInvoiceDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCustomerPaymentsAsync()
    {
        if (await _context.CustomerPayments.AnyAsync()) { _logger.LogInformation("CustomerPayments already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var invoices = await _context.SalesInvoices.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var orders = await _context.SalesOrders.ToListAsync();
        var methods = new[] { "Cash", "Bank Transfer", "Credit Card", "Cheque", "Mobile Payment" };

        var payments = new List<CustomerPayment>();
        for (int i = 1; i <= 20; i++)
        {
            var paymentDate = new DateTime(2026, 3, 1).AddDays(i);
            payments.Add(new CustomerPayment
            {
                PaymentNumber = $"CP{i:D3}",
                CustomerID = customers[(i-1)%customers.Count].CustomerID,
                SalesOrderID = orders[(i-1)%orders.Count].SalesOrderID,
                InvoiceID = invoices[(i-1)%invoices.Count].InvoiceID,
                ProcessedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                PaymentDate = paymentDate,
                Amount = 1000+(i*100),
                PaymentMethod = methods[(i-1)%methods.Length],
                ReferenceNumber = $"REF{i:D6}",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.CustomerPayments.AddRange(payments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseOrdersAsync()
    {
        if (await _context.PurchaseOrders.AnyAsync()) { _logger.LogInformation("PurchaseOrders already seeded. Skipping."); return; }
        var suppliers = await _context.Suppliers.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var warehouses = await _context.Warehouses.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Draft", "Submitted", "Approved", "Received", "Cancelled" };

        var orders = new List<PurchaseOrder>();
        for (int i = 1; i <= 20; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 6 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var orderDate = new DateTime(year, month, 1).AddDays(i - 1);
            var subtotal = 1500+(i*150);
            orders.Add(new PurchaseOrder
            {
                OrderNumber = $"PO{i:D3}",
                SupplierID = suppliers[(i-1)%suppliers.Count].SupplierID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                WarehouseID = warehouses[(i-1)%warehouses.Count].WarehouseID,
                RequestedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                ApprovedByEmployeeID = i>10 ? employees[(i%employees.Count)].EmployeeID : null,
                OrderDate = orderDate,
                ExpectedDeliveryDate = orderDate.AddDays(14),
                SubTotal = subtotal,
                TaxAmount = subtotal*0.1m,
                DiscountAmount = i%5==0 ? 75 : 0,
                TotalAmount = subtotal*1.1m-(i%5==0?75:0),
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.PurchaseOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseOrderDetailsAsync()
    {
        if (await _context.PurchaseOrderDetails.AnyAsync()) { _logger.LogInformation("PurchaseOrderDetails already seeded. Skipping."); return; }
        var orders = await _context.PurchaseOrders.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<PurchaseOrderDetail>();
        for (int i = 1; i <= 20; i++)
        {
            var quantity = 5+(i%15);
            var unitPrice = 30+(i*5);
            var lineTotal = quantity*unitPrice;
            details.Add(new PurchaseOrderDetail
            {
                PurchaseOrderID = orders[(i-1)%orders.Count].PurchaseOrderID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountPercent = i%6==0 ? 5 : 0,
                LineTotal = lineTotal-(lineTotal*(i%6==0?0.05m:0)),
                QuantityReceived = i>15 ? quantity : 0
            });
        }
        _context.PurchaseOrderDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseReceiptsAsync()
    {
        if (await _context.PurchaseReceipts.AnyAsync()) { _logger.LogInformation("PurchaseReceipts already seeded. Skipping."); return; }
        var suppliers = await _context.Suppliers.ToListAsync();
        var warehouses = await _context.Warehouses.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var orders = await _context.PurchaseOrders.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Draft", "Received", "Verified", "Completed" };

        var receipts = new List<PurchaseReceipt>();
        for (int i = 1; i <= 20; i++)
        {
            var receiptDate = new DateTime(2026, 3, 1).AddDays(i);
            receipts.Add(new PurchaseReceipt
            {
                ReceiptNumber = $"PR{i:D3}",
                PurchaseOrderID = orders[(i-1)%orders.Count].PurchaseOrderID,
                SupplierID = suppliers[(i-1)%suppliers.Count].SupplierID,
                WarehouseID = warehouses[(i-1)%warehouses.Count].WarehouseID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                ReceivedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                ReceiptDate = receiptDate,
                TotalAmount = 1200+(i*120),
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.PurchaseReceipts.AddRange(receipts);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseReceiptDetailsAsync()
    {
        if (await _context.PurchaseReceiptDetails.AnyAsync()) { _logger.LogInformation("PurchaseReceiptDetails already seeded. Skipping."); return; }
        var receipts = await _context.PurchaseReceipts.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<PurchaseReceiptDetail>();
        for (int i = 1; i <= 20; i++)
        {
            var quantity = 10+(i%20);
            var unitCost = 25+(i*3);
            details.Add(new PurchaseReceiptDetail
            {
                ReceiptID = receipts[(i-1)%receipts.Count].ReceiptID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitCost = unitCost,
                LineTotal = quantity*unitCost
            });
        }
        _context.PurchaseReceiptDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedInventoriesAsync()
    {
        if (await _context.Inventories.AnyAsync()) { _logger.LogInformation("Inventories already seeded. Skipping."); return; }
        var products = await _context.Products.ToListAsync();
        var warehouses = await _context.Warehouses.ToListAsync();
        var branches = await _context.Branches.ToListAsync();

        var inventories = new List<Inventory>();
        for (int i = 1; i <= 30; i++)
        {
            var qoh = 50+(i*10);
            var qres = i%3==0 ? 10 : 0;
            inventories.Add(new Inventory
            {
                ProductID = products[(i-1)%products.Count].ProductID,
                WarehouseID = warehouses[(i-1)%warehouses.Count].WarehouseID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                QuantityOnHand = qoh,
                QuantityReserved = qres,
                QuantityAvailable = qoh-qres,
                ReorderPoint = 20,
                ReorderQuantity = 50,
                AverageCost = 25+(i*2),
                LastUpdatedAt = DateTime.UtcNow
            });
        }
        _context.Inventories.AddRange(inventories);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesReturnsAsync()
    {
        if (await _context.SalesReturns.AnyAsync()) { _logger.LogInformation("SalesReturns already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var orders = await _context.SalesOrders.ToListAsync();
        var invoices = await _context.SalesInvoices.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Pending", "Approved", "Rejected", "Completed" };

        var returns = new List<SalesReturn>();
        for (int i = 1; i <= 15; i++)
        {
            var returnDate = new DateTime(2026, 5, 1).AddDays(i);
            returns.Add(new SalesReturn
            {
                ReturnNumber = $"SR{i:D3}",
                SalesOrderID = orders[(i-1)%orders.Count].SalesOrderID,
                InvoiceID = invoices[(i-1)%invoices.Count].InvoiceID,
                CustomerID = customers[(i-1)%customers.Count].CustomerID,
                ProcessedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                ReturnDate = returnDate,
                TotalAmount = 100+(i*10),
                Reason = "Defective",
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.SalesReturns.AddRange(returns);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSalesReturnDetailsAsync()
    {
        if (await _context.SalesReturnDetails.AnyAsync()) { _logger.LogInformation("SalesReturnDetails already seeded. Skipping."); return; }
        var returns = await _context.SalesReturns.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<SalesReturnDetail>();
        for (int i = 1; i <= 15; i++)
        {
            var quantity = 1+(i%5);
            var unitPrice = 30+(i*5);
            details.Add(new SalesReturnDetail
            {
                ReturnID = returns[(i-1)%returns.Count].ReturnID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                LineTotal = quantity*unitPrice,
                Reason = $"Return reason {i}"
            });
        }
        _context.SalesReturnDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSupplierPaymentsAsync()
    {
        if (await _context.SupplierPayments.AnyAsync()) { _logger.LogInformation("SupplierPayments already seeded. Skipping."); return; }
        var suppliers = await _context.Suppliers.ToListAsync();
        var invoices = await _context.PurchaseInvoices.ToListAsync();
        var orders = await _context.PurchaseOrders.ToListAsync();
        var receipts = await _context.PurchaseReceipts.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var methods = new[] { "Cash", "Bank Transfer", "Credit Card", "Cheque" };

        var payments = new List<SupplierPayment>();
        for (int i = 1; i <= 15; i++)
        {
            var paymentDate = new DateTime(2026, 5, 1).AddDays(i);
            payments.Add(new SupplierPayment
            {
                PaymentNumber = $"SP{i:D3}",
                PurchaseOrderID = orders[(i-1)%orders.Count].PurchaseOrderID,
                ReceiptID = receipts[(i-1)%receipts.Count].ReceiptID,
                SupplierID = suppliers[(i-1)%suppliers.Count].SupplierID,
                ProcessedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                PaymentDate = paymentDate,
                Amount = 800+(i*80),
                PaymentMethod = methods[(i-1)%methods.Length],
                ReferenceNumber = $"SUP{i:D6}",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.SupplierPayments.AddRange(payments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedExpensesAsync()
    {
        if (await _context.Expenses.AnyAsync()) { _logger.LogInformation("Expenses already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var categories = await _context.ExpenseCategories.ToListAsync();

        var expenses = new List<Expense>();
        for (int i = 1; i <= 60; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 12 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var day = (i - 1) % 28 + 1;
            var expenseDate = new DateTime(year, month, day);
            var isApproved = i % 2 == 0;
            expenses.Add(new Expense
            {
                ExpenseNumber = $"EX{i:D4}",
                EmployeeID = employees[(i - 1) % employees.Count].EmployeeID,
                BranchID = branches[(i - 1) % branches.Count].BranchID,
                CategoryID = categories[(i - 1) % categories.Count].ExpenseCategoryID,
                ExpenseDate = expenseDate,
                Amount = 50 + (i * 5),
                Description = $"Expense {i}",
                Status = isApproved ? "Approved" : "Pending",
                ApprovedByEmployeeID = isApproved ? employees[i % employees.Count].EmployeeID : null,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Expenses.AddRange(expenses);
        await _context.SaveChangesAsync();
    }

    private async Task SeedAttendancesAsync()
    {
        if (await _context.Attendances.AnyAsync()) { _logger.LogInformation("Attendances already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Present", "Absent", "Late", "On Leave" };

        var attendances = new List<Attendance>();
        for (int i = 1; i <= 30; i++)
        {
            var attendanceDate = new DateTime(2026, 6, 1).AddDays(i%30);
            attendances.Add(new Attendance
            {
                EmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                AttendanceDate = attendanceDate,
                CheckInTime = attendanceDate.AddHours(8+(i%2)),
                CheckOutTime = attendanceDate.AddHours(17+(i%3)),
                Status = statuses[(i-1)%statuses.Length]
            });
        }
        _context.Attendances.AddRange(attendances);
        await _context.SaveChangesAsync();
    }

    private async Task SeedLeaveRequestsAsync()
    {
        if (await _context.LeaveRequests.AnyAsync()) { _logger.LogInformation("LeaveRequests already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var types = new[] { "Annual", "Sick", "Personal", "Maternity", "Paternity" };
        var statuses = new[] { "Pending", "Approved", "Rejected", "Cancelled" };

        var requests = new List<LeaveRequest>();
        for (int i = 1; i <= 20; i++)
        {
            var startDate = new DateTime(2026, 7, 1).AddDays(i);
            requests.Add(new LeaveRequest
            {
                EmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                LeaveType = types[(i-1)%types.Length],
                StartDate = startDate,
                EndDate = startDate.AddDays(i%5+1),
                TotalDays = i%5+1,
                Reason = $"Leave request {i}",
                Status = statuses[(i-1)%statuses.Length],
                ApprovedByEmployeeID = i>10 ? employees[(i%employees.Count)].EmployeeID : null,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.LeaveRequests.AddRange(requests);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPerformanceReviewsAsync()
    {
        if (await _context.PerformanceReviews.AnyAsync()) { _logger.LogInformation("PerformanceReviews already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();

        var reviews = new List<PerformanceReview>();
        for (int i = 1; i <= 20; i++)
        {
            var reviewDate = new DateTime(2026, 6, 1).AddDays(i*7);
            reviews.Add(new PerformanceReview
            {
                EmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                ReviewedByEmployeeID = employees[(i%employees.Count)].EmployeeID,
                ReviewDate = reviewDate,
                ReviewPeriodStart = reviewDate.AddMonths(-6),
                ReviewPeriodEnd = reviewDate,
                OverallRating = (i%5)+1,
                Strengths = $"Strength {i}",
                AreasForImprovement = $"Improvement {i}",
                Comments = $"Review comments {i}",
                Goals = $"Goals {i}",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.PerformanceReviews.AddRange(reviews);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPayrollsAsync()
    {
        if (await _context.Payrolls.AnyAsync()) { _logger.LogInformation("Payrolls already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var statuses = new[] { "Pending", "Processing", "Paid" };

        var payrolls = new List<Payroll>();
        for (int i = 1; i <= 30; i++)
        {
            var payrollDate = new DateTime(2026, 1, 1).AddMonths((i-1)%12);
            var baseSalary = 3000+(i*100);
            var overtime = 200+(i*20);
            var bonus = i%3==0 ? 500 : 0;
            var allowance = 200+(i*20);
            var deduction = 100+(i*10);
            var tax = (baseSalary+overtime+bonus+allowance)*0.1m;
            payrolls.Add(new Payroll
            {
                EmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                PayrollPeriod = payrollDate.ToString("yyyy-MM"),
                PeriodStartDate = new DateTime(payrollDate.Year, payrollDate.Month, 1),
                PeriodEndDate = new DateTime(payrollDate.Year, payrollDate.Month, DateTime.DaysInMonth(payrollDate.Year, payrollDate.Month)),
                PaymentDate = payrollDate.AddDays(25),
                BaseSalary = baseSalary,
                OvertimeAmount = overtime,
                BonusAmount = bonus,
                AllowanceAmount = allowance,
                DeductionAmount = deduction,
                TaxAmount = tax,
                NetSalary = baseSalary+overtime+bonus+allowance-deduction-tax,
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Payrolls.AddRange(payrolls);
        await _context.SaveChangesAsync();
    }

    private async Task SeedJobOpeningsAsync()
    {
        if (await _context.JobOpenings.AnyAsync()) { _logger.LogInformation("JobOpenings already seeded. Skipping."); return; }
        var departments = await _context.Departments.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Open", "Closed", "On Hold", "Filled" };
        var types = new[] { "FullTime", "PartTime", "Contract", "Internship" };
        var titles = new[] { "Manager", "Analyst", "Specialist", "Coordinator", "Associate", "Lead", "Engineer", "Consultant", "Director", "Executive" };
        var cities = new[] { "Ho Chi Minh City", "Hanoi", "Da Nang" };

        var openings = new List<JobOpening>();
        for (int i = 1; i <= 15; i++)
        {
            var postDate = new DateTime(2026, 8, 1).AddDays(i);
            openings.Add(new JobOpening
            {
                Title = $"{titles[(i-1)%titles.Length]} Position {i}",
                DepartmentID = departments[(i-1)%departments.Count].DepartmentID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                CreatedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                EmploymentType = types[(i-1)%types.Length],
                Location = cities[(i-1)%cities.Length],
                SalaryMin = 500+(i*50),
                SalaryMax = 1000+(i*100),
                NumberOfPositions = (i%3)+1,
                JobDescription = $"Job description for position {i}",
                Requirements = $"Requirements for position {i}",
                PostedDate = postDate,
                ClosingDate = postDate.AddDays(30),
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.JobOpenings.AddRange(openings);
        await _context.SaveChangesAsync();
    }

    private async Task SeedApplicantsAsync()
    {
        if (await _context.Applicants.AnyAsync()) { _logger.LogInformation("Applicants already seeded. Skipping."); return; }
        var jobs = await _context.JobOpenings.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var statuses = new[] { "Applied", "Screening", "Interview", "Offer", "Rejected", "Hired" };
        var firstNames = new[] { "Tran", "Le", "Pham", "Hoang", "Vu", "Do", "Ngo", "Bui" };
        var lastNames = new[] { "An", "Binh", "Cuc", "Dung", "Em", "Phuc", "Quang", "Sang" };

        var applicants = new List<Applicant>();
        for (int i = 1; i <= 20; i++)
        {
            var applyDate = new DateTime(2026, 9, 1).AddDays(i);
            applicants.Add(new Applicant
            {
                JobOpeningID = jobs[(i-1)%jobs.Count].JobOpeningID,
                FullName = $"{firstNames[(i-1)%firstNames.Length]} {lastNames[(i-1)%lastNames.Length]}",
                Gender = i%2==0 ? "Male" : "Female",
                DateOfBirth = new DateTime(1990+(i%15), (i%12)+1, (i%28)+1),
                Phone = $"+84-90{i%100:D2}-XXX-XXXX",
                Email = $"applicant{i}@email.com",
                Address = $"{100+i} Applicant Street",
                Status = statuses[(i-1)%statuses.Length],
                AppliedDate = applyDate,
                ReferredByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Applicants.AddRange(applicants);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSupportTicketsAsync()
    {
        if (await _context.SupportTickets.AnyAsync()) { _logger.LogInformation("SupportTickets already seeded. Skipping."); return; }
        var customers = await _context.Customers.ToListAsync();
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var priorities = new[] { "Low", "Medium", "High", "Critical" };
        var statuses = new[] { "Open", "In Progress", "Resolved", "Closed" };
        var types = new[] { "Technical", "Billing", "General", "Sales", "Service" };

        var tickets = new List<SupportTicket>();
        for (int i = 1; i <= 60; i++)
        {
            var month = (i - 1) % 12 + 1;
            var day = (i - 1) % 28 + 1;
            var createDate = new DateTime(2026, month, day);
            var isResolved = i <= 40;
            tickets.Add(new SupportTicket
            {
                TicketNumber = $"TK{i:D4}",
                CustomerID = customers[(i - 1) % customers.Count].CustomerID,
                AssignedToEmployeeID = employees[(i - 1) % employees.Count].EmployeeID,
                BranchID = branches[(i - 1) % branches.Count].BranchID,
                Subject = $"Support ticket {i}",
                TicketType = types[(i - 1) % types.Length],
                Priority = priorities[(i - 1) % priorities.Length],
                Status = isResolved ? statuses[2] : (i % 2 == 0 ? statuses[1] : statuses[0]),
                Description = $"Ticket description {i}",
                ResolvedDate = isResolved ? createDate.AddDays(3) : null,
                CreatedAt = createDate
            });
        }
        _context.SupportTickets.AddRange(tickets);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseInvoicesAsync()
    {
        if (await _context.PurchaseInvoices.AnyAsync()) { _logger.LogInformation("PurchaseInvoices already seeded. Skipping."); return; }
        var suppliers = await _context.Suppliers.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var orders = await _context.PurchaseOrders.ToListAsync();
        var statuses = new[] { "Pending", "Approved", "Paid", "Overdue" };

        var invoices = new List<PurchaseInvoice>();
        for (int i = 1; i <= 15; i++)
        {
            var invoiceDate = new DateTime(2026, 10, 1).AddDays(i);
            var subtotal = 1500+(i*150);
            var total = subtotal*1.1m;
            invoices.Add(new PurchaseInvoice
            {
                InvoiceNumber = $"PINV{i:D3}",
                PurchaseOrderID = orders[(i-1)%orders.Count].PurchaseOrderID,
                SupplierID = suppliers[(i-1)%suppliers.Count].SupplierID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                InvoiceDate = invoiceDate,
                DueDate = invoiceDate.AddDays(30),
                SubTotal = subtotal,
                TaxAmount = subtotal*0.1m,
                TotalAmount = total,
                AmountPaid = i>10 ? total : 0,
                AmountDue = i>10 ? 0 : total,
                PaymentStatus = i>10 ? "Paid" : "Pending",
                Status = statuses[(i-1)%statuses.Length],
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.PurchaseInvoices.AddRange(invoices);
        await _context.SaveChangesAsync();
    }

    private async Task SeedPurchaseInvoiceDetailsAsync()
    {
        if (await _context.PurchaseInvoiceDetails.AnyAsync()) { _logger.LogInformation("PurchaseInvoiceDetails already seeded. Skipping."); return; }
        var invoices = await _context.PurchaseInvoices.ToListAsync();
        var products = await _context.Products.ToListAsync();

        var details = new List<PurchaseInvoiceDetail>();
        for (int i = 1; i <= 15; i++)
        {
            var quantity = 8+(i%12);
            var unitCost = 40+(i*4);
            details.Add(new PurchaseInvoiceDetail
            {
                InvoiceID = invoices[(i-1)%invoices.Count].InvoiceID,
                ProductID = products[(i-1)%products.Count].ProductID,
                Quantity = quantity,
                UnitCost = unitCost,
                DiscountPercent = 0,
                LineTotal = quantity*unitCost,
                Description = $"Invoice detail {i}"
            });
        }
        _context.PurchaseInvoiceDetails.AddRange(details);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMarketingSpendDailiesAsync()
    {
        if (await _context.MarketingSpendDailies.AnyAsync()) { _logger.LogInformation("MarketingSpendDailies already seeded. Skipping."); return; }
        var campaigns = await _context.MarketingCampaigns.ToListAsync();

        var spends = new List<MarketingSpendDaily>();
        for (int i = 1; i <= 36; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 12 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var spendDate = new DateTime(year, month, 15);
            var amount = 500 + (i * 50);
            var impressions = 5000 + (i * 500);
            var clicks = 200 + (i * 20);
            spends.Add(new MarketingSpendDaily
            {
                CampaignID = campaigns[(i - 1) % campaigns.Count].CampaignID,
                SpendDate = spendDate,
                Amount = amount,
                Impressions = impressions,
                Clicks = clicks,
                Conversions = i % 5,
                CPM = impressions > 0 ? amount / impressions * 1000 : 0,
                CPC = clicks > 0 ? amount / clicks : 0
            });
        }
        _context.MarketingSpendDailies.AddRange(spends);
        await _context.SaveChangesAsync();
    }

    private async Task SeedInventorySnapshotsAsync()
    {
        if (await _context.InventorySnapshots.AnyAsync()) { _logger.LogInformation("InventorySnapshots already seeded. Skipping."); return; }
        var products = await _context.Products.ToListAsync();
        var warehouses = await _context.Warehouses.ToListAsync();
        var branches = await _context.Branches.ToListAsync();

        var snapshots = new List<InventorySnapshot>();
        for (int i = 1; i <= 20; i++)
        {
            var baseYear = 2025;
            var offsetMonths = (i - 1) % 6 + 10;
            var year = baseYear + offsetMonths / 12;
            var month = offsetMonths % 12 == 0 ? 12 : offsetMonths % 12;
            var snapshotDate = new DateTime(year, month, 1).AddDays((i - 1) % 28);
            var qoh = 100+(i*10);
            var avgCost = 30+(i*3);
            snapshots.Add(new InventorySnapshot
            {
                ProductID = products[(i-1)%products.Count].ProductID,
                WarehouseID = warehouses[(i-1)%warehouses.Count].WarehouseID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                SnapshotDate = snapshotDate,
                QuantityOnHand = qoh,
                AverageCost = avgCost,
                TotalValue = qoh*avgCost
            });
        }
        _context.InventorySnapshots.AddRange(snapshots);
        await _context.SaveChangesAsync();
    }

    private async Task SeedOpportunityStageHistoriesAsync()
    {
        if (await _context.OpportunityStageHistories.AnyAsync()) { _logger.LogInformation("OpportunityStageHistories already seeded. Skipping."); return; }
        var opportunities = await _context.Opportunities.ToListAsync();
        var stages = await _context.OpportunityStages.ToListAsync();
        var employees = await _context.Employees.ToListAsync();

        var histories = new List<OpportunityStageHistory>();
        for (int i = 1; i <= 20; i++)
        {
            var changeDate = new DateTime(2026, 3, 1).AddDays(i - 10);
            histories.Add(new OpportunityStageHistory
            {
                OpportunityID = opportunities[(i-1)%opportunities.Count].OpportunityID,
                FromStageID = stages[(i-1)%stages.Count].StageID,
                ToStageID = stages[(i%stages.Count)].StageID,
                ChangedByEmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                ChangedAt = changeDate,
                Note = $"Stage change {i}"
            });
        }
        _context.OpportunityStageHistories.AddRange(histories);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDimDatesAsync()
    {
        if (await _context.DimDates.AnyAsync()) { _logger.LogInformation("DimDates already seeded. Skipping."); return; }
        var dates = new List<DimDate>();
        var startDate = new DateTime(2026, 1, 1);

        for (int i = 0; i < 365; i++)
        {
            var currentDate = startDate.AddDays(i);
            dates.Add(new DimDate
            {
                FullDate = currentDate,
                DayNumberOfMonth = (byte)currentDate.Day,
                DayName = currentDate.DayOfWeek.ToString(),
                WeekNumberOfYear = (byte)((currentDate.DayOfYear/7)+1),
                MonthNumber = (byte)currentDate.Month,
                MonthName = currentDate.ToString("MMMM"),
                QuarterNumber = (byte)((currentDate.Month-1)/3+1),
                YearNumber = (short)currentDate.Year,
                YearMonth = currentDate.ToString("yyyy-MM"),
                IsWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday,
                IsMonthEnd = currentDate.Day == DateTime.DaysInMonth(currentDate.Year, currentDate.Month),
                IsQuarterEnd = currentDate.Month % 3 == 0 && currentDate.Day == DateTime.DaysInMonth(currentDate.Year, currentDate.Month),
                IsYearEnd = currentDate.Month == 12 && currentDate.Day == 31
            });
        }
        _context.DimDates.AddRange(dates);
        await _context.SaveChangesAsync();
    }

    private async Task SeedKpiTargetsAsync()
    {
        if (await _context.KpiTargets.AnyAsync()) { _logger.LogInformation("KpiTargets already seeded. Skipping."); return; }
        var employees = await _context.Employees.ToListAsync();
        var branches = await _context.Branches.ToListAsync();
        var departments = await _context.Departments.ToListAsync();
        var types = new[] { "Revenue", "Sales", "Customer", "Operational" };
        var names = new[] { "Monthly Sales", "Quarterly Revenue", "New Customers", "Customer Satisfaction" };

        var targets = new List<KpiTarget>();
        for (int i = 1; i <= 20; i++)
        {
            var targetDate = new DateTime(2026, 1, 1).AddMonths((i-1)%12);
            targets.Add(new KpiTarget
            {
                EmployeeID = employees[(i-1)%employees.Count].EmployeeID,
                BranchID = branches[(i-1)%branches.Count].BranchID,
                DepartmentID = departments[(i-1)%departments.Count].DepartmentID,
                KpiName = names[(i-1)%names.Length],
                KpiType = types[(i-1)%types.Length],
                TargetValue = 50000+(i*5000),
                ActualValue = i>10 ? 40000+(i*4000) : null,
                AchievementPercent = i>10 ? 80+(i%20) : null,
                StartDate = targetDate,
                EndDate = targetDate.AddMonths(3),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.KpiTargets.AddRange(targets);
        await _context.SaveChangesAsync();
    }
}
