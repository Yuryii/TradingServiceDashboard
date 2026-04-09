using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;
using Dashboard.Services.Interfaces;

namespace Dashboard.Controllers;

[Authorize(Policy = "SalesPolicy")]
public class SalesCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;
    private readonly INotificationService _notificationService;

    public SalesCrudController(
        ApplicationDbContext context,
        ExcelCrudService excelService,
        INotificationService notificationService)
    {
        _context = context;
        _excelService = excelService;
        _notificationService = notificationService;
    }

    // GET: /SalesCrud
    public IActionResult Index()
    {
        return View();
    }

    #region Customers

    public async Task<IActionResult> Customers()
    {
        var items = await _context.Customers
            .Include(c => c.CustomerGroup)
            .AsNoTracking()
            .OrderBy(c => c.CustomerName)
            .Select(c => new CustomerListVM
            {
                CustomerID = c.CustomerID,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                CustomerType = c.CustomerType,
                CustomerGroupName = c.CustomerGroup != null ? c.CustomerGroup.GroupName : null,
                Phone = c.Phone,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
        return View("Customers/Index", items);
    }

    public IActionResult Customers_Create()
    {
        ViewBag.CustomerGroups = _context.CustomerGroups.Where(g => g.IsActive).OrderBy(g => g.GroupName).ToList();
        ViewBag.Regions = _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToList();
        ViewBag.Branches = _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToList();
        return View("Customers/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customers_Create(CustomerCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CustomerGroups = _context.CustomerGroups.Where(g => g.IsActive).ToList();
            ViewBag.Regions = _context.Regions.Where(r => r.IsActive).ToList();
            ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();
            return View("Customers/Create", model);
        }

        var entity = new Customer
        {
            CustomerCode = model.CustomerCode,
            CustomerName = model.CustomerName,
            CustomerType = model.CustomerType,
            CustomerGroupID = model.CustomerGroupID,
            RegionID = model.RegionID,
            BranchID = model.BranchID,
            Industry = model.Industry,
            TaxCode = model.TaxCode,
            Phone = model.Phone,
            Email = model.Email,
            AddressLine = model.AddressLine,
            City = model.City,
            Province = model.Province,
            Country = model.Country,
            JoinDate = model.JoinDate,
            CreditLimit = model.CreditLimit,
            PaymentTermDays = model.PaymentTermDays,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(entity);
        await _context.SaveChangesAsync();

        // 立即发送通知给 Executive
        await _notificationService.SendToRoleAsync("Executive", new NotificationSignalDto
        {
            Title = "New customer",
            Message = $"New customer \"{model.CustomerName}\" was created by {User.Identity?.Name}",
            Category = "Sales",
            Severity = "Info",
            IconClass = "bx-user-plus",
            IconBgClass = "bg-label-success",
            ActionUrl = "/SalesCrud/Customers"
        });

        TempData["SuccessMessage"] = "Customer created successfully.";
        return RedirectToAction(nameof(Customers));
    }

    public async Task<IActionResult> Customers_Edit(int id)
    {
        var entity = await _context.Customers.FindAsync(id);
        if (entity == null) return NotFound();

        ViewBag.CustomerGroups = _context.CustomerGroups.Where(g => g.IsActive).ToList();
        ViewBag.Regions = _context.Regions.Where(r => r.IsActive).ToList();
        ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();

        var vm = new CustomerEditVM
        {
            CustomerID = entity.CustomerID,
            CustomerCode = entity.CustomerCode,
            CustomerName = entity.CustomerName,
            CustomerType = entity.CustomerType,
            CustomerGroupID = entity.CustomerGroupID,
            RegionID = entity.RegionID,
            BranchID = entity.BranchID,
            Industry = entity.Industry,
            TaxCode = entity.TaxCode,
            Phone = entity.Phone,
            Email = entity.Email,
            AddressLine = entity.AddressLine,
            City = entity.City,
            Province = entity.Province,
            Country = entity.Country,
            JoinDate = entity.JoinDate,
            CreditLimit = entity.CreditLimit,
            PaymentTermDays = entity.PaymentTermDays,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
        return View("Customers/Edit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customers_Edit(CustomerEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CustomerGroups = _context.CustomerGroups.Where(g => g.IsActive).ToList();
            ViewBag.Regions = _context.Regions.Where(r => r.IsActive).ToList();
            ViewBag.Branches = _context.Branches.Where(b => b.IsActive).ToList();
            return View("Customers/Edit", model);
        }

        var entity = await _context.Customers.FindAsync(model.CustomerID);
        if (entity == null) return NotFound();

        entity.CustomerCode = model.CustomerCode;
        entity.CustomerName = model.CustomerName;
        entity.CustomerType = model.CustomerType;
        entity.CustomerGroupID = model.CustomerGroupID;
        entity.RegionID = model.RegionID;
        entity.BranchID = model.BranchID;
        entity.Industry = model.Industry;
        entity.TaxCode = model.TaxCode;
        entity.Phone = model.Phone;
        entity.Email = model.Email;
        entity.AddressLine = model.AddressLine;
        entity.City = model.City;
        entity.Province = model.Province;
        entity.Country = model.Country;
        entity.JoinDate = model.JoinDate;
        entity.CreditLimit = model.CreditLimit;
        entity.PaymentTermDays = model.PaymentTermDays;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Customers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customers_Delete(int id)
    {
        var entity = await _context.Customers.FindAsync(id);
        if (entity == null) return NotFound();

        _context.Customers.Remove(entity);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer deleted successfully.";
        return RedirectToAction(nameof(Customers));
    }

    #endregion

    #region Export
    public async Task<IActionResult> Customers_Export() => await Export("Customer", "Customer");
    public async Task<IActionResult> CustomerGroups_Export() => await Export("CustomerGroup", "CustomerGroup");
    public async Task<IActionResult> SalesChannels_Export() => await Export("SalesChannel", "SalesChannel");
    public async Task<IActionResult> OpportunityStages_Export() => await Export("OpportunityStage", "OpportunityStage");
    public async Task<IActionResult> Opportunities_Export() => await Export("Opportunity", "Opportunity");
    public async Task<IActionResult> Quotes_Export() => await Export("Quote", "Quote");
    public async Task<IActionResult> SalesOrders_Export() => await Export("SalesOrder", "SalesOrder");
    public async Task<IActionResult> SalesInvoices_Export() => await Export("SalesInvoice", "SalesInvoice");
    public async Task<IActionResult> SalesReturns_Export() => await Export("SalesReturn", "SalesReturn");
    public async Task<IActionResult> CustomerPayments_Export() => await Export("CustomerPayment", "CustomerPayment");
    #endregion

    #region CustomerGroups

    public async Task<IActionResult> CustomerGroups()
    {
        var items = await _context.CustomerGroups.AsNoTracking().OrderBy(g => g.GroupName)
            .Select(g => new CustomerGroupListVM { CustomerGroupID = g.CustomerGroupID, GroupCode = g.GroupCode, GroupName = g.GroupName, Description = g.Description, IsActive = g.IsActive })
            .ToListAsync();
        return View("CustomerGroups/Index", items);
    }

    public IActionResult CustomerGroups_Create() => View("CustomerGroups/Create");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerGroups_Create(CustomerGroupCreateVM model)
    {
        if (!ModelState.IsValid) return View("CustomerGroups/Create", model);
        _context.CustomerGroups.Add(new CustomerGroup { GroupCode = model.GroupCode, GroupName = model.GroupName, Description = model.Description, IsActive = model.IsActive });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer group created successfully.";
        return RedirectToAction(nameof(CustomerGroups));
    }

    public async Task<IActionResult> CustomerGroups_Edit(int id)
    {
        var e = await _context.CustomerGroups.FindAsync(id);
        if (e == null) return NotFound();
        return View("CustomerGroups/Edit", new CustomerGroupEditVM { CustomerGroupID = e.CustomerGroupID, GroupCode = e.GroupCode, GroupName = e.GroupName, Description = e.Description, IsActive = e.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerGroups_Edit(CustomerGroupEditVM model)
    {
        if (!ModelState.IsValid) return View("CustomerGroups/Edit", model);
        var e = await _context.CustomerGroups.FindAsync(model.CustomerGroupID);
        if (e == null) return NotFound();
        e.GroupCode = model.GroupCode; e.GroupName = model.GroupName; e.Description = model.Description; e.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer group updated successfully.";
        return RedirectToAction(nameof(CustomerGroups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerGroups_Delete(int id)
    {
        var e = await _context.CustomerGroups.FindAsync(id);
        if (e == null) return NotFound();
        _context.CustomerGroups.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer group deleted successfully.";
        return RedirectToAction(nameof(CustomerGroups));
    }

    #endregion

    #region SalesChannels

    public async Task<IActionResult> SalesChannels()
    {
        var items = await _context.SalesChannels.AsNoTracking().OrderBy(c => c.ChannelName)
            .Select(c => new SalesChannelListVM { SalesChannelID = c.SalesChannelID, ChannelCode = c.ChannelCode, ChannelName = c.ChannelName, Description = c.Description, IsActive = c.IsActive })
            .ToListAsync();
        return View("SalesChannels/Index", items);
    }

    public IActionResult SalesChannels_Create() => View("SalesChannels/Create");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesChannels_Create(SalesChannelCreateVM model)
    {
        if (!ModelState.IsValid) return View("SalesChannels/Create", model);
        _context.SalesChannels.Add(new SalesChannel { ChannelCode = model.ChannelCode, ChannelName = model.ChannelName, Description = model.Description, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales channel created successfully.";
        return RedirectToAction(nameof(SalesChannels));
    }

    public async Task<IActionResult> SalesChannels_Edit(int id)
    {
        var e = await _context.SalesChannels.FindAsync(id);
        if (e == null) return NotFound();
        return View("SalesChannels/Edit", new SalesChannelEditVM { SalesChannelID = e.SalesChannelID, ChannelCode = e.ChannelCode, ChannelName = e.ChannelName, Description = e.Description, IsActive = e.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesChannels_Edit(SalesChannelEditVM model)
    {
        if (!ModelState.IsValid) return View("SalesChannels/Edit", model);
        var e = await _context.SalesChannels.FindAsync(model.SalesChannelID);
        if (e == null) return NotFound();
        e.ChannelCode = model.ChannelCode; e.ChannelName = model.ChannelName; e.Description = model.Description; e.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales channel updated successfully.";
        return RedirectToAction(nameof(SalesChannels));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesChannels_Delete(int id)
    {
        var e = await _context.SalesChannels.FindAsync(id);
        if (e == null) return NotFound();
        _context.SalesChannels.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales channel deleted successfully.";
        return RedirectToAction(nameof(SalesChannels));
    }

    #endregion

    #region OpportunityStages

    public async Task<IActionResult> OpportunityStages()
    {
        var items = await _context.OpportunityStages.AsNoTracking().OrderBy(s => s.StageOrder)
            .Select(s => new OpportunityStageListVM { StageID = s.StageID, StageCode = s.StageCode, StageName = s.StageName, StageOrder = s.StageOrder, IsClosedStage = s.IsClosedStage, IsWonStage = s.IsWonStage, IsLostStage = s.IsLostStage })
            .ToListAsync();
        return View("OpportunityStages/Index", items);
    }

    public IActionResult OpportunityStages_Create() => View("OpportunityStages/Create");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpportunityStages_Create(OpportunityStageCreateVM model)
    {
        if (!ModelState.IsValid) return View("OpportunityStages/Create", model);
        _context.OpportunityStages.Add(new OpportunityStage { StageCode = model.StageCode, StageName = model.StageName, StageOrder = model.StageOrder, IsClosedStage = model.IsClosedStage, IsWonStage = model.IsWonStage, IsLostStage = model.IsLostStage });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity stage created successfully.";
        return RedirectToAction(nameof(OpportunityStages));
    }

    public async Task<IActionResult> OpportunityStages_Edit(int id)
    {
        var e = await _context.OpportunityStages.FindAsync(id);
        if (e == null) return NotFound();
        return View("OpportunityStages/Edit", new OpportunityStageEditVM { StageID = e.StageID, StageCode = e.StageCode, StageName = e.StageName, StageOrder = e.StageOrder, IsClosedStage = e.IsClosedStage, IsWonStage = e.IsWonStage, IsLostStage = e.IsLostStage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpportunityStages_Edit(OpportunityStageEditVM model)
    {
        if (!ModelState.IsValid) return View("OpportunityStages/Edit", model);
        var e = await _context.OpportunityStages.FindAsync(model.StageID);
        if (e == null) return NotFound();
        e.StageCode = model.StageCode; e.StageName = model.StageName; e.StageOrder = model.StageOrder; e.IsClosedStage = model.IsClosedStage; e.IsWonStage = model.IsWonStage; e.IsLostStage = model.IsLostStage;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity stage updated successfully.";
        return RedirectToAction(nameof(OpportunityStages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpportunityStages_Delete(int id)
    {
        var e = await _context.OpportunityStages.FindAsync(id);
        if (e == null) return NotFound();
        _context.OpportunityStages.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity stage deleted successfully.";
        return RedirectToAction(nameof(OpportunityStages));
    }

    #endregion

    #region Opportunities

    public async Task<IActionResult> Opportunities()
    {
        var items = await _context.Opportunities
            .Include(o => o.Customer)
            .AsNoTracking().OrderByDescending(o => o.CreatedAt)
            .Select(o => new OpportunityListVM { 
                OpportunityID = o.OpportunityID, 
                OpportunityCode = o.OpportunityCode, 
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null, 
                StageID = o.StageID, 
                StageName = _context.OpportunityStages.Where(s => s.StageID == o.StageID).Select(s => s.StageName).FirstOrDefault(), 
                Status = o.Status, 
                EstimatedValue = o.EstimatedValue, 
                Probability = o.Probability, 
                ExpectedCloseDate = o.ExpectedCloseDate, 
                CreatedAt = o.CreatedAt 
            })
            .ToListAsync();
        return View("Opportunities/Index", items);
    }

    public async Task<IActionResult> Opportunities_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Stages = await _context.OpportunityStages.OrderBy(s => s.StageOrder).ToListAsync();
        return View("Opportunities/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Opportunities_Create(OpportunityCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.Stages = await _context.OpportunityStages.OrderBy(s => s.StageOrder).ToListAsync();
            return View("Opportunities/Create", model);
        }

        _context.Opportunities.Add(new Opportunity
        {
            OpportunityCode = model.OpportunityCode, CustomerID = model.CustomerID, LeadID = model.LeadID,
            OwnerEmployeeID = model.OwnerEmployeeID, BranchID = model.BranchID, StageID = model.StageID,
            SourceChannel = model.SourceChannel, ExpectedCloseDate = model.ExpectedCloseDate, ActualCloseDate = model.ActualCloseDate,
            EstimatedValue = model.EstimatedValue, Probability = model.Probability, Status = model.Status,
            WonReason = model.WonReason, LostReason = model.LostReason, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity created successfully.";
        return RedirectToAction(nameof(Opportunities));
    }

    public async Task<IActionResult> Opportunities_Edit(int id)
    {
        var e = await _context.Opportunities.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.Stages = await _context.OpportunityStages.OrderBy(s => s.StageOrder).ToListAsync();
        return View("Opportunities/Edit", new OpportunityEditVM { OpportunityID = e.OpportunityID, OpportunityCode = e.OpportunityCode, CustomerID = e.CustomerID, LeadID = e.LeadID, OwnerEmployeeID = e.OwnerEmployeeID, BranchID = e.BranchID, StageID = e.StageID, SourceChannel = e.SourceChannel, ExpectedCloseDate = e.ExpectedCloseDate, ActualCloseDate = e.ActualCloseDate, EstimatedValue = e.EstimatedValue, Probability = e.Probability, Status = e.Status, WonReason = e.WonReason, LostReason = e.LostReason, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Opportunities_Edit(OpportunityEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.Stages = await _context.OpportunityStages.OrderBy(s => s.StageOrder).ToListAsync();
            return View("Opportunities/Edit", model);
        }
        var e = await _context.Opportunities.FindAsync(model.OpportunityID);
        if (e == null) return NotFound();
        e.OpportunityCode = model.OpportunityCode; e.CustomerID = model.CustomerID; e.LeadID = model.LeadID;
        e.OwnerEmployeeID = model.OwnerEmployeeID; e.BranchID = model.BranchID; e.StageID = model.StageID;
        e.SourceChannel = model.SourceChannel; e.ExpectedCloseDate = model.ExpectedCloseDate; e.ActualCloseDate = model.ActualCloseDate;
        e.EstimatedValue = model.EstimatedValue; e.Probability = model.Probability; e.Status = model.Status;
        e.WonReason = model.WonReason; e.LostReason = model.LostReason; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Opportunities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Opportunities_Delete(int id)
    {
        var e = await _context.Opportunities.FindAsync(id);
        if (e == null) return NotFound();
        _context.Opportunities.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        return RedirectToAction(nameof(Opportunities));
    }

    #endregion

    #region Quotes

    public async Task<IActionResult> Quotes()
    {
        var items = await _context.Quotes
            .Include(q => q.Customer).Include(q => q.Opportunity)
            .AsNoTracking().OrderByDescending(q => q.QuoteDate)
            .Select(q => new QuoteListVM { QuoteID = q.QuoteID, QuoteNumber = q.QuoteNumber, CustomerName = q.Customer != null ? q.Customer.CustomerName : null, OpportunityCode = q.Opportunity != null ? q.Opportunity.OpportunityCode : null, TotalAmount = q.TotalAmount, Status = q.Status, QuoteDate = q.QuoteDate, ValidUntilDate = q.ValidUntilDate })
            .ToListAsync();
        return View("Quotes/Index", items);
    }

    public async Task<IActionResult> Quotes_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View("Quotes/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quotes_Create(QuoteCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.Opportunities = await _context.Opportunities.ToListAsync();
            return View("Quotes/Create", model);
        }
        _context.Quotes.Add(new Quote { QuoteNumber = model.QuoteNumber, OpportunityID = model.OpportunityID, CustomerID = model.CustomerID, BranchID = model.BranchID, SalesEmployeeID = model.SalesEmployeeID, QuoteDate = model.QuoteDate, ValidUntilDate = model.ValidUntilDate, SubTotal = model.SubTotal, TaxAmount = model.TaxAmount, TotalAmount = model.TotalAmount, DiscountPercent = model.DiscountPercent, TermsAndConditions = model.TermsAndConditions, Status = model.Status, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Quote created successfully.";
        return RedirectToAction(nameof(Quotes));
    }

    public async Task<IActionResult> Quotes_Edit(int id)
    {
        var e = await _context.Quotes.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.ToListAsync();
        return View("Quotes/Edit", new QuoteEditVM { QuoteID = e.QuoteID, QuoteNumber = e.QuoteNumber, OpportunityID = e.OpportunityID, CustomerID = e.CustomerID, BranchID = e.BranchID, SalesEmployeeID = e.SalesEmployeeID, QuoteDate = e.QuoteDate, ValidUntilDate = e.ValidUntilDate, SubTotal = e.SubTotal, TaxAmount = e.TaxAmount, TotalAmount = e.TotalAmount, DiscountPercent = e.DiscountPercent, TermsAndConditions = e.TermsAndConditions, Status = e.Status, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quotes_Edit(QuoteEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.Opportunities = await _context.Opportunities.ToListAsync();
            return View("Quotes/Edit", model);
        }
        var e = await _context.Quotes.FindAsync(model.QuoteID);
        if (e == null) return NotFound();
        e.QuoteNumber = model.QuoteNumber; e.OpportunityID = model.OpportunityID; e.CustomerID = model.CustomerID; e.BranchID = model.BranchID; e.SalesEmployeeID = model.SalesEmployeeID; e.QuoteDate = model.QuoteDate; e.ValidUntilDate = model.ValidUntilDate; e.SubTotal = model.SubTotal; e.TaxAmount = model.TaxAmount; e.TotalAmount = model.TotalAmount; e.DiscountPercent = model.DiscountPercent; e.TermsAndConditions = model.TermsAndConditions; e.Status = model.Status; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Quote updated successfully.";
        return RedirectToAction(nameof(Quotes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quotes_Delete(int id)
    {
        var e = await _context.Quotes.FindAsync(id);
        if (e == null) return NotFound();
        _context.Quotes.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Quote deleted successfully.";
        return RedirectToAction(nameof(Quotes));
    }

    #endregion

    #region SalesOrders

    public async Task<IActionResult> SalesOrders()
    {
        var items = await _context.SalesOrders
            .Include(o => o.Customer).Include(o => o.Opportunity)
            .AsNoTracking().OrderByDescending(o => o.OrderDate)
            .Select(o => new SalesOrderListVM { SalesOrderID = o.SalesOrderID, OrderNumber = o.OrderNumber, CustomerName = o.Customer != null ? o.Customer.CustomerName : null, OpportunityCode = o.Opportunity != null ? o.Opportunity.OpportunityCode : null, TotalAmount = o.TotalAmount, PaymentStatus = o.PaymentStatus, DeliveryStatus = o.DeliveryStatus, OrderDate = o.OrderDate })
            .ToListAsync();
        return View("SalesOrders/Index", items);
    }

    public async Task<IActionResult> SalesOrders_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.SalesChannels = await _context.SalesChannels.Where(s => s.IsActive).OrderBy(s => s.ChannelName).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.Where(o => o.Status == "Open").OrderByDescending(o => o.CreatedAt).ToListAsync();
        ViewBag.Quotes = await _context.Quotes.Where(q => q.Status == "Accepted").OrderByDescending(q => q.QuoteDate).ToListAsync();
        return View("SalesOrders/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesOrders_Create(SalesOrderCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesChannels = await _context.SalesChannels.Where(s => s.IsActive).ToListAsync();
            ViewBag.Opportunities = await _context.Opportunities.Where(o => o.Status == "Open").ToListAsync();
            ViewBag.Quotes = await _context.Quotes.Where(q => q.Status == "Accepted").ToListAsync();
            return View("SalesOrders/Create", model);
        }
        _context.SalesOrders.Add(new SalesOrder { OrderNumber = model.OrderNumber, OpportunityID = model.OpportunityID, QuoteID = model.QuoteID, CustomerID = model.CustomerID, BranchID = model.BranchID, SalesChannelID = model.SalesChannelID, SalesEmployeeID = model.SalesEmployeeID, OrderDate = model.OrderDate, DeliveryDate = model.DeliveryDate, SubTotal = model.SubTotal, TaxAmount = model.TaxAmount, DiscountAmount = model.DiscountAmount, TotalAmount = model.TotalAmount, PaidAmount = model.PaidAmount, PaymentStatus = model.PaymentStatus, DeliveryStatus = model.DeliveryStatus, ShippingAddress = model.ShippingAddress, ShippingCity = model.ShippingCity, ShippingProvince = model.ShippingProvince, ShippingCountry = model.ShippingCountry, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales order created successfully.";
        return RedirectToAction(nameof(SalesOrders));
    }

    public async Task<IActionResult> SalesOrders_Edit(int id)
    {
        var e = await _context.SalesOrders.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.SalesChannels = await _context.SalesChannels.Where(s => s.IsActive).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.ToListAsync();
        ViewBag.Quotes = await _context.Quotes.ToListAsync();
        return View("SalesOrders/Edit", new SalesOrderEditVM { SalesOrderID = e.SalesOrderID, OrderNumber = e.OrderNumber, OpportunityID = e.OpportunityID, QuoteID = e.QuoteID, CustomerID = e.CustomerID, BranchID = e.BranchID, SalesChannelID = e.SalesChannelID, SalesEmployeeID = e.SalesEmployeeID, OrderDate = e.OrderDate, DeliveryDate = e.DeliveryDate, SubTotal = e.SubTotal, TaxAmount = e.TaxAmount, DiscountAmount = e.DiscountAmount, TotalAmount = e.TotalAmount, PaidAmount = e.PaidAmount, PaymentStatus = e.PaymentStatus, DeliveryStatus = e.DeliveryStatus, ShippingAddress = e.ShippingAddress, ShippingCity = e.ShippingCity, ShippingProvince = e.ShippingProvince, ShippingCountry = e.ShippingCountry, Notes = e.Notes, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesOrders_Edit(SalesOrderEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesChannels = await _context.SalesChannels.Where(s => s.IsActive).ToListAsync();
            ViewBag.Opportunities = await _context.Opportunities.ToListAsync();
            ViewBag.Quotes = await _context.Quotes.ToListAsync();
            return View("SalesOrders/Edit", model);
        }
        var e = await _context.SalesOrders.FindAsync(model.SalesOrderID);
        if (e == null) return NotFound();
        e.OrderNumber = model.OrderNumber; e.OpportunityID = model.OpportunityID; e.QuoteID = model.QuoteID; e.CustomerID = model.CustomerID; e.BranchID = model.BranchID; e.SalesChannelID = model.SalesChannelID; e.SalesEmployeeID = model.SalesEmployeeID; e.OrderDate = model.OrderDate; e.DeliveryDate = model.DeliveryDate; e.SubTotal = model.SubTotal; e.TaxAmount = model.TaxAmount; e.DiscountAmount = model.DiscountAmount; e.TotalAmount = model.TotalAmount; e.PaidAmount = model.PaidAmount; e.PaymentStatus = model.PaymentStatus; e.DeliveryStatus = model.DeliveryStatus; e.ShippingAddress = model.ShippingAddress; e.ShippingCity = model.ShippingCity; e.ShippingProvince = model.ShippingProvince; e.ShippingCountry = model.ShippingCountry; e.Notes = model.Notes; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales order updated successfully.";
        return RedirectToAction(nameof(SalesOrders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesOrders_Delete(int id)
    {
        var e = await _context.SalesOrders.FindAsync(id);
        if (e == null) return NotFound();
        _context.SalesOrders.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales order deleted successfully.";
        return RedirectToAction(nameof(SalesOrders));
    }

    #endregion

    #region SalesInvoices

    public async Task<IActionResult> SalesInvoices()
    {
        var items = await _context.SalesInvoices
            .Include(i => i.Customer).Include(i => i.SalesOrder)
            .AsNoTracking().OrderByDescending(i => i.InvoiceDate)
            .Select(i => new SalesInvoiceListVM { InvoiceID = i.InvoiceID, InvoiceNumber = i.InvoiceNumber, CustomerName = i.Customer != null ? i.Customer.CustomerName : null, SalesOrderNumber = i.SalesOrder != null ? i.SalesOrder.OrderNumber : null, TotalAmount = i.TotalAmount, PaidAmount = i.PaidAmount, OutstandingAmount = i.OutstandingAmount, PaymentStatus = i.PaymentStatus, InvoiceDate = i.InvoiceDate })
            .ToListAsync();
        return View("SalesInvoices/Index", items);
    }

    public async Task<IActionResult> SalesInvoices_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.Where(o => o.DeliveryStatus == "Delivered").OrderByDescending(o => o.OrderDate).ToListAsync();
        return View("SalesInvoices/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesInvoices_Create(SalesInvoiceCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.Where(o => o.DeliveryStatus == "Delivered").ToListAsync();
            return View("SalesInvoices/Create", model);
        }
        _context.SalesInvoices.Add(new SalesInvoice { InvoiceNumber = model.InvoiceNumber, SalesOrderID = model.SalesOrderID, CustomerID = model.CustomerID, BranchID = model.BranchID, SalesEmployeeID = model.SalesEmployeeID, InvoiceDate = model.InvoiceDate, DueDate = model.DueDate, SubTotal = model.SubTotal, TaxAmount = model.TaxAmount, DiscountAmount = model.DiscountAmount, TotalAmount = model.TotalAmount, PaidAmount = model.PaidAmount, OutstandingAmount = model.OutstandingAmount, PaymentStatus = model.PaymentStatus, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales invoice created successfully.";
        return RedirectToAction(nameof(SalesInvoices));
    }

    public async Task<IActionResult> SalesInvoices_Edit(int id)
    {
        var e = await _context.SalesInvoices.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
        return View("SalesInvoices/Edit", new SalesInvoiceEditVM { InvoiceID = e.InvoiceID, InvoiceNumber = e.InvoiceNumber, SalesOrderID = e.SalesOrderID, CustomerID = e.CustomerID, BranchID = e.BranchID, SalesEmployeeID = e.SalesEmployeeID, InvoiceDate = e.InvoiceDate, DueDate = e.DueDate, SubTotal = e.SubTotal, TaxAmount = e.TaxAmount, DiscountAmount = e.DiscountAmount, TotalAmount = e.TotalAmount, PaidAmount = e.PaidAmount, OutstandingAmount = e.OutstandingAmount, PaymentStatus = e.PaymentStatus, Notes = e.Notes, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesInvoices_Edit(SalesInvoiceEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
            return View("SalesInvoices/Edit", model);
        }
        var e = await _context.SalesInvoices.FindAsync(model.InvoiceID);
        if (e == null) return NotFound();
        e.InvoiceNumber = model.InvoiceNumber; e.SalesOrderID = model.SalesOrderID; e.CustomerID = model.CustomerID; e.BranchID = model.BranchID; e.SalesEmployeeID = model.SalesEmployeeID; e.InvoiceDate = model.InvoiceDate; e.DueDate = model.DueDate; e.SubTotal = model.SubTotal; e.TaxAmount = model.TaxAmount; e.DiscountAmount = model.DiscountAmount; e.TotalAmount = model.TotalAmount; e.PaidAmount = model.PaidAmount; e.OutstandingAmount = model.OutstandingAmount; e.PaymentStatus = model.PaymentStatus; e.Notes = model.Notes; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales invoice updated successfully.";
        return RedirectToAction(nameof(SalesInvoices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesInvoices_Delete(int id)
    {
        var e = await _context.SalesInvoices.FindAsync(id);
        if (e == null) return NotFound();
        _context.SalesInvoices.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales invoice deleted successfully.";
        return RedirectToAction(nameof(SalesInvoices));
    }

    #endregion

    #region SalesReturns

    public async Task<IActionResult> SalesReturns()
    {
        var items = await _context.SalesReturns
            .Include(r => r.Customer).Include(r => r.SalesOrder)
            .AsNoTracking().OrderByDescending(r => r.ReturnDate)
            .Select(r => new SalesReturnListVM { ReturnID = r.ReturnID, ReturnNumber = r.ReturnNumber, CustomerName = r.Customer != null ? r.Customer.CustomerName : null, SalesOrderNumber = r.SalesOrder != null ? r.SalesOrder.OrderNumber : null, TotalAmount = r.TotalAmount, Status = r.Status, ReturnDate = r.ReturnDate })
            .ToListAsync();
        return View("SalesReturns/Index", items);
    }

    public async Task<IActionResult> SalesReturns_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
        ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
        return View("SalesReturns/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesReturns_Create(SalesReturnCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
            ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
            return View("SalesReturns/Create", model);
        }
        _context.SalesReturns.Add(new SalesReturn { ReturnNumber = model.ReturnNumber, SalesOrderID = model.SalesOrderID, InvoiceID = model.InvoiceID, CustomerID = model.CustomerID, BranchID = model.BranchID, ProcessedByEmployeeID = model.ProcessedByEmployeeID, ReturnDate = model.ReturnDate, TotalAmount = model.TotalAmount, Status = model.Status, Reason = model.Reason, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales return created successfully.";
        return RedirectToAction(nameof(SalesReturns));
    }

    public async Task<IActionResult> SalesReturns_Edit(int id)
    {
        var e = await _context.SalesReturns.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
        ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
        return View("SalesReturns/Edit", new SalesReturnEditVM { ReturnID = e.ReturnID, ReturnNumber = e.ReturnNumber, SalesOrderID = e.SalesOrderID, InvoiceID = e.InvoiceID, CustomerID = e.CustomerID, BranchID = e.BranchID, ProcessedByEmployeeID = e.ProcessedByEmployeeID, ReturnDate = e.ReturnDate, TotalAmount = e.TotalAmount, Status = e.Status, Reason = e.Reason, Notes = e.Notes, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesReturns_Edit(SalesReturnEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
            ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
            return View("SalesReturns/Edit", model);
        }
        var e = await _context.SalesReturns.FindAsync(model.ReturnID);
        if (e == null) return NotFound();
        e.ReturnNumber = model.ReturnNumber; e.SalesOrderID = model.SalesOrderID; e.InvoiceID = model.InvoiceID; e.CustomerID = model.CustomerID; e.BranchID = model.BranchID; e.ProcessedByEmployeeID = model.ProcessedByEmployeeID; e.ReturnDate = model.ReturnDate; e.TotalAmount = model.TotalAmount; e.Status = model.Status; e.Reason = model.Reason; e.Notes = model.Notes; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales return updated successfully.";
        return RedirectToAction(nameof(SalesReturns));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalesReturns_Delete(int id)
    {
        var e = await _context.SalesReturns.FindAsync(id);
        if (e == null) return NotFound();
        _context.SalesReturns.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Sales return deleted successfully.";
        return RedirectToAction(nameof(SalesReturns));
    }

    #endregion

    #region CustomerPayments

    public async Task<IActionResult> CustomerPayments()
    {
        var items = await _context.CustomerPayments
            .Include(p => p.Customer)
            .AsNoTracking().OrderByDescending(p => p.PaymentDate)
            .Select(p => new CustomerPaymentListVM { PaymentID = p.PaymentID, PaymentNumber = p.PaymentNumber, CustomerName = p.Customer != null ? p.Customer.CustomerName : null, Amount = p.Amount, PaymentMethod = p.PaymentMethod, PaymentDate = p.PaymentDate })
            .ToListAsync();
        return View("CustomerPayments/Index", items);
    }

    public async Task<IActionResult> CustomerPayments_Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
        ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
        return View("CustomerPayments/Create");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerPayments_Create(CustomerPaymentCreateVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
            ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
            return View("CustomerPayments/Create", model);
        }
        _context.CustomerPayments.Add(new CustomerPayment { PaymentNumber = model.PaymentNumber, SalesOrderID = model.SalesOrderID, InvoiceID = model.InvoiceID, ReturnID = model.ReturnID, CustomerID = model.CustomerID, BranchID = model.BranchID, ProcessedByEmployeeID = model.ProcessedByEmployeeID, PaymentDate = model.PaymentDate, Amount = model.Amount, PaymentMethod = model.PaymentMethod, ReferenceNumber = model.ReferenceNumber, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer payment created successfully.";
        return RedirectToAction(nameof(CustomerPayments));
    }

    public async Task<IActionResult> CustomerPayments_Edit(int id)
    {
        var e = await _context.CustomerPayments.FindAsync(id);
        if (e == null) return NotFound();
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
        ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
        return View("CustomerPayments/Edit", new CustomerPaymentEditVM { PaymentID = e.PaymentID, PaymentNumber = e.PaymentNumber, SalesOrderID = e.SalesOrderID, InvoiceID = e.InvoiceID, ReturnID = e.ReturnID, CustomerID = e.CustomerID, BranchID = e.BranchID, ProcessedByEmployeeID = e.ProcessedByEmployeeID, PaymentDate = e.PaymentDate, Amount = e.Amount, PaymentMethod = e.PaymentMethod, ReferenceNumber = e.ReferenceNumber, Notes = e.Notes, CreatedAt = e.CreatedAt });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerPayments_Edit(CustomerPaymentEditVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.ToListAsync();
            ViewBag.SalesInvoices = await _context.SalesInvoices.ToListAsync();
            return View("CustomerPayments/Edit", model);
        }
        var e = await _context.CustomerPayments.FindAsync(model.PaymentID);
        if (e == null) return NotFound();
        e.PaymentNumber = model.PaymentNumber; e.SalesOrderID = model.SalesOrderID; e.InvoiceID = model.InvoiceID; e.ReturnID = model.ReturnID; e.CustomerID = model.CustomerID; e.BranchID = model.BranchID; e.ProcessedByEmployeeID = model.ProcessedByEmployeeID; e.PaymentDate = model.PaymentDate; e.Amount = model.Amount; e.PaymentMethod = model.PaymentMethod; e.ReferenceNumber = model.ReferenceNumber; e.Notes = model.Notes;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer payment updated successfully.";
        return RedirectToAction(nameof(CustomerPayments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomerPayments_Delete(int id)
    {
        var e = await _context.CustomerPayments.FindAsync(id);
        if (e == null) return NotFound();
        _context.CustomerPayments.Remove(e);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Customer payment deleted successfully.";
        return RedirectToAction(nameof(CustomerPayments));
    }

    #endregion

    public async Task<IActionResult> Export(string? entityType, string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            var actionName = RouteData.Values["action"]?.ToString() ?? "";
            if (actionName.EndsWith("_Export", StringComparison.OrdinalIgnoreCase))
                entityType = actionName[..^7];
        }

        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest("entityType is required");

        var entityName2 = entityName ?? entityType;
        try
        {
            var bytes = await _excelService.ExportAsync(entityType);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{entityName2}_Export_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Export error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
