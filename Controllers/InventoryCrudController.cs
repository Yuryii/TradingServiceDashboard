using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers;

[Authorize(Policy = "InventoryPolicy")]
public class InventoryCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public InventoryCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new InventoryCrudIndexVM
        {
            ProductCount = await _context.Products.CountAsync(),
            CategoryCount = await _context.ProductCategories.CountAsync(),
            WarehouseCount = await _context.Warehouses.CountAsync(),
            SupplierCount = await _context.Suppliers.CountAsync(),
            PurchaseOrderCount = await _context.PurchaseOrders.CountAsync(),
            PurchaseReceiptCount = await _context.PurchaseReceipts.CountAsync(),
            PurchaseInvoiceCount = await _context.PurchaseInvoices.CountAsync(),
            SupplierPaymentCount = await _context.SupplierPayments.CountAsync()
        };
        return View(model);
    }

    #region Export
    public async Task<IActionResult> Product_Export() => await Export("Product", "Product");
    public async Task<IActionResult> ProductCategory_Export() => await Export("ProductCategory", "ProductCategory");
    public async Task<IActionResult> Warehouse_Export() => await Export("Warehouse", "Warehouse");
    public async Task<IActionResult> Supplier_Export() => await Export("Supplier", "Supplier");
    public async Task<IActionResult> PurchaseOrder_Export() => await Export("PurchaseOrder", "PurchaseOrder");
    public async Task<IActionResult> PurchaseReceipt_Export() => await Export("PurchaseReceipt", "PurchaseReceipt");
    public async Task<IActionResult> PurchaseInvoice_Export() => await Export("PurchaseInvoice", "PurchaseInvoice");
    public async Task<IActionResult> SupplierPayment_Export() => await Export("SupplierPayment", "SupplierPayment");
    #endregion

    #region Products
    public async Task<IActionResult> Products()
    {
        var items = await _context.Products.Include(p => p.Category).AsNoTracking().OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductListVM { ProductID = p.ProductID, ProductCode = p.ProductCode, ProductName = p.ProductName, CategoryName = p.Category != null ? p.Category.CategoryName : null, ProductType = p.ProductType, UnitOfMeasure = p.UnitOfMeasure, SalePrice = p.SalePrice, CostPrice = p.CostPrice, IsActive = p.IsActive })
            .ToListAsync();
        return View("Products/Index", items);
    }
    public async Task<IActionResult> Products_Create() { ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(); return View("Products/Create", new ProductCreateVM()); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Products_Create(ProductCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync(); return View("Products/Create", model); }
        _context.Products.Add(new Product { ProductCode = model.ProductCode, ProductName = model.ProductName, CategoryID = model.CategoryID, ProductType = model.ProductType, UnitOfMeasure = model.UnitOfMeasure, Brand = model.Brand, SalePrice = model.SalePrice, CostPrice = model.CostPrice, ReorderLevel = model.ReorderLevel, MaxStockLevel = model.MaxStockLevel, IsStockItem = model.IsStockItem, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Product '{model.ProductName}' created successfully."; return RedirectToAction(nameof(Products));
    }
    public async Task<IActionResult> Products_Edit(int id) {
        var entity = await _context.Products.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        return View("Products/Edit", new ProductEditVM { ProductID = entity.ProductID, ProductCode = entity.ProductCode, ProductName = entity.ProductName, CategoryID = entity.CategoryID, ProductType = entity.ProductType, UnitOfMeasure = entity.UnitOfMeasure, Brand = entity.Brand, SalePrice = entity.SalePrice, CostPrice = entity.CostPrice, ReorderLevel = entity.ReorderLevel, MaxStockLevel = entity.MaxStockLevel, IsStockItem = entity.IsStockItem, IsActive = entity.IsActive, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Products_Edit(ProductEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync(); return View("Products/Edit", model); }
        var entity = await _context.Products.FindAsync(model.ProductID); if (entity == null) return NotFound();
        entity.ProductCode = model.ProductCode; entity.ProductName = model.ProductName; entity.CategoryID = model.CategoryID; entity.ProductType = model.ProductType; entity.UnitOfMeasure = model.UnitOfMeasure; entity.Brand = model.Brand; entity.SalePrice = model.SalePrice; entity.CostPrice = model.CostPrice; entity.ReorderLevel = model.ReorderLevel; entity.MaxStockLevel = model.MaxStockLevel; entity.IsStockItem = model.IsStockItem; entity.IsActive = model.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Product '{model.ProductName}' updated successfully."; return RedirectToAction(nameof(Products));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Products_Delete(int id) {
        var entity = await _context.Products.FindAsync(id); if (entity == null) return NotFound();
        _context.Products.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Product '{entity.ProductName}' deleted successfully."; return RedirectToAction(nameof(Products));
    }
    #endregion

    #region ProductCategories
    public async Task<IActionResult> ProductCategories() {
        var items = await _context.ProductCategories.Include(c => c.ParentCategory).AsNoTracking().OrderBy(c => c.CategoryName)
            .Select(c => new ProductCategoryListVM { CategoryID = c.CategoryID, CategoryCode = c.CategoryCode, CategoryName = c.CategoryName, ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null, Description = c.Description, IsActive = c.IsActive })
            .ToListAsync();
        return View("ProductCategories/Index", items);
    }
    public async Task<IActionResult> ProductCategories_Create() { ViewBag.ParentCategories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(); return View("ProductCategories/Create", new ProductCategoryCreateVM()); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ProductCategories_Create(ProductCategoryCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.ParentCategories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync(); return View("ProductCategories/Create", model); }
        _context.ProductCategories.Add(new ProductCategory { CategoryCode = model.CategoryCode, CategoryName = model.CategoryName, ParentCategoryID = model.ParentCategoryID, Description = model.Description, IsActive = model.IsActive });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{model.CategoryName}' created successfully."; return RedirectToAction(nameof(ProductCategories));
    }
    public async Task<IActionResult> ProductCategories_Edit(int id) {
        var entity = await _context.ProductCategories.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.ParentCategories = await _context.ProductCategories.Where(c => c.IsActive && c.CategoryID != id).OrderBy(c => c.CategoryName).ToListAsync();
        return View("ProductCategories/Edit", new ProductCategoryEditVM { CategoryID = entity.CategoryID, CategoryCode = entity.CategoryCode, CategoryName = entity.CategoryName, ParentCategoryID = entity.ParentCategoryID, Description = entity.Description, IsActive = entity.IsActive });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ProductCategories_Edit(ProductCategoryEditVM model) {
        if (!ModelState.IsValid) { ViewBag.ParentCategories = await _context.ProductCategories.Where(c => c.IsActive && c.CategoryID != model.CategoryID).ToListAsync(); return View("ProductCategories/Edit", model); }
        var entity = await _context.ProductCategories.FindAsync(model.CategoryID); if (entity == null) return NotFound();
        entity.CategoryCode = model.CategoryCode; entity.CategoryName = model.CategoryName; entity.ParentCategoryID = model.ParentCategoryID; entity.Description = model.Description; entity.IsActive = model.IsActive;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{model.CategoryName}' updated successfully."; return RedirectToAction(nameof(ProductCategories));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> ProductCategories_Delete(int id) {
        var entity = await _context.ProductCategories.FindAsync(id); if (entity == null) return NotFound();
        _context.ProductCategories.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Category '{entity.CategoryName}' deleted successfully."; return RedirectToAction(nameof(ProductCategories));
    }
    #endregion

    #region Warehouses
    public async Task<IActionResult> Warehouses() {
        var items = await _context.Warehouses.Include(w => w.Branch).AsNoTracking().OrderBy(w => w.WarehouseName)
            .Select(w => new WarehouseListVM { WarehouseID = w.WarehouseID, WarehouseCode = w.WarehouseCode, WarehouseName = w.WarehouseName, BranchName = w.Branch != null ? w.Branch.BranchName : null, City = w.City, Province = w.Province, IsActive = w.IsActive })
            .ToListAsync();
        return View("Warehouses/Index", items);
    }
    public async Task<IActionResult> Warehouses_Create() { ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync(); return View("Warehouses/Create", new WarehouseCreateVM()); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Warehouses_Create(WarehouseCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("Warehouses/Create", model); }
        _context.Warehouses.Add(new Warehouse { WarehouseCode = model.WarehouseCode, WarehouseName = model.WarehouseName, BranchID = model.BranchID, AddressLine = model.AddressLine, City = model.City, Province = model.Province, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Warehouse '{model.WarehouseName}' created successfully."; return RedirectToAction(nameof(Warehouses));
    }
    public async Task<IActionResult> Warehouses_Edit(int id) {
        var entity = await _context.Warehouses.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        return View("Warehouses/Edit", new WarehouseEditVM { WarehouseID = entity.WarehouseID, WarehouseCode = entity.WarehouseCode, WarehouseName = entity.WarehouseName, BranchID = entity.BranchID, AddressLine = entity.AddressLine, City = entity.City, Province = entity.Province, IsActive = entity.IsActive, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Warehouses_Edit(WarehouseEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("Warehouses/Edit", model); }
        var entity = await _context.Warehouses.FindAsync(model.WarehouseID); if (entity == null) return NotFound();
        entity.WarehouseCode = model.WarehouseCode; entity.WarehouseName = model.WarehouseName; entity.BranchID = model.BranchID; entity.AddressLine = model.AddressLine; entity.City = model.City; entity.Province = model.Province; entity.IsActive = model.IsActive;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Warehouse '{model.WarehouseName}' updated successfully."; return RedirectToAction(nameof(Warehouses));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Warehouses_Delete(int id) {
        var entity = await _context.Warehouses.FindAsync(id); if (entity == null) return NotFound();
        _context.Warehouses.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Warehouse '{entity.WarehouseName}' deleted successfully."; return RedirectToAction(nameof(Warehouses));
    }
    #endregion

    #region Suppliers
    public async Task<IActionResult> Suppliers() {
        var items = await _context.Suppliers.Include(s => s.Region).AsNoTracking().OrderBy(s => s.SupplierName)
            .Select(s => new SupplierListVM { SupplierID = s.SupplierID, SupplierCode = s.SupplierCode, SupplierName = s.SupplierName, SupplierType = s.SupplierType, Phone = s.Phone, Email = s.Email, IsActive = s.IsActive })
            .ToListAsync();
        return View("Suppliers/Index", items);
    }
    public async Task<IActionResult> Suppliers_Create() { ViewBag.Regions = await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToListAsync(); return View("Suppliers/Create", new SupplierCreateVM()); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Suppliers_Create(SupplierCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Regions = await _context.Regions.Where(r => r.IsActive).ToListAsync(); return View("Suppliers/Create", model); }
        _context.Suppliers.Add(new Supplier { SupplierCode = model.SupplierCode, SupplierName = model.SupplierName, SupplierType = model.SupplierType, RegionID = model.RegionID, TaxCode = model.TaxCode, Phone = model.Phone, Email = model.Email, AddressLine = model.AddressLine, City = model.City, Province = model.Province, Country = model.Country, PaymentTermDays = model.PaymentTermDays, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier '{model.SupplierName}' created successfully."; return RedirectToAction(nameof(Suppliers));
    }
    public async Task<IActionResult> Suppliers_Edit(int id) {
        var entity = await _context.Suppliers.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.Regions = await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.RegionName).ToListAsync();
        return View("Suppliers/Edit", new SupplierEditVM { SupplierID = entity.SupplierID, SupplierCode = entity.SupplierCode, SupplierName = entity.SupplierName, SupplierType = entity.SupplierType, RegionID = entity.RegionID, TaxCode = entity.TaxCode, Phone = entity.Phone, Email = entity.Email, AddressLine = entity.AddressLine, City = entity.City, Province = entity.Province, Country = entity.Country, PaymentTermDays = entity.PaymentTermDays, IsActive = entity.IsActive, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Suppliers_Edit(SupplierEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Regions = await _context.Regions.Where(r => r.IsActive).ToListAsync(); return View("Suppliers/Edit", model); }
        var entity = await _context.Suppliers.FindAsync(model.SupplierID); if (entity == null) return NotFound();
        entity.SupplierCode = model.SupplierCode; entity.SupplierName = model.SupplierName; entity.SupplierType = model.SupplierType; entity.RegionID = model.RegionID; entity.TaxCode = model.TaxCode; entity.Phone = model.Phone; entity.Email = model.Email; entity.AddressLine = model.AddressLine; entity.City = model.City; entity.Province = model.Province; entity.Country = model.Country; entity.PaymentTermDays = model.PaymentTermDays; entity.IsActive = model.IsActive; entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier '{model.SupplierName}' updated successfully."; return RedirectToAction(nameof(Suppliers));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Suppliers_Delete(int id) {
        var entity = await _context.Suppliers.FindAsync(id); if (entity == null) return NotFound();
        _context.Suppliers.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier '{entity.SupplierName}' deleted successfully."; return RedirectToAction(nameof(Suppliers));
    }
    #endregion

    #region PurchaseOrders
    public async Task<IActionResult> PurchaseOrders() {
        var items = await _context.PurchaseOrders.Include(p => p.Supplier).AsNoTracking().OrderByDescending(p => p.OrderDate)
            .Select(p => new PurchaseOrderListVM { PurchaseOrderID = p.PurchaseOrderID, OrderNumber = p.OrderNumber, SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null, TotalAmount = p.TotalAmount, PaymentStatus = p.PaymentStatus, DeliveryStatus = p.DeliveryStatus, Status = p.Status, OrderDate = p.OrderDate })
            .ToListAsync();
        return View("PurchaseOrders/Index", items);
    }
    public async Task<IActionResult> PurchaseOrders_Create() {
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        return View("PurchaseOrders/Create", new PurchaseOrderCreateVM());
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseOrders_Create(PurchaseOrderCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("PurchaseOrders/Create", model); }
        _context.PurchaseOrders.Add(new PurchaseOrder { OrderNumber = model.OrderNumber, SupplierID = model.SupplierID, WarehouseID = model.WarehouseID, BranchID = model.BranchID, RequestedByEmployeeID = model.RequestedByEmployeeID, ApprovedByEmployeeID = model.ApprovedByEmployeeID, OrderDate = model.OrderDate, ExpectedDeliveryDate = model.ExpectedDeliveryDate, SubTotal = model.SubTotal, TaxAmount = model.TaxAmount, DiscountAmount = model.DiscountAmount, TotalAmount = model.TotalAmount, PaymentStatus = model.PaymentStatus, DeliveryStatus = model.DeliveryStatus, Notes = model.Notes, Status = model.Status, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase order '{model.OrderNumber}' created successfully."; return RedirectToAction(nameof(PurchaseOrders));
    }
    public async Task<IActionResult> PurchaseOrders_Edit(int id) {
        var entity = await _context.PurchaseOrders.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        return View("PurchaseOrders/Edit", new PurchaseOrderEditVM { PurchaseOrderID = entity.PurchaseOrderID, OrderNumber = entity.OrderNumber, SupplierID = entity.SupplierID, WarehouseID = entity.WarehouseID, BranchID = entity.BranchID, RequestedByEmployeeID = entity.RequestedByEmployeeID, ApprovedByEmployeeID = entity.ApprovedByEmployeeID, OrderDate = entity.OrderDate, ExpectedDeliveryDate = entity.ExpectedDeliveryDate, ActualDeliveryDate = entity.ActualDeliveryDate, SubTotal = entity.SubTotal, TaxAmount = entity.TaxAmount, DiscountAmount = entity.DiscountAmount, TotalAmount = entity.TotalAmount, PaymentStatus = entity.PaymentStatus, DeliveryStatus = entity.DeliveryStatus, Notes = entity.Notes, Status = entity.Status, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseOrders_Edit(PurchaseOrderEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("PurchaseOrders/Edit", model); }
        var entity = await _context.PurchaseOrders.FindAsync(model.PurchaseOrderID); if (entity == null) return NotFound();
        entity.OrderNumber = model.OrderNumber; entity.SupplierID = model.SupplierID; entity.WarehouseID = model.WarehouseID; entity.BranchID = model.BranchID; entity.RequestedByEmployeeID = model.RequestedByEmployeeID; entity.ApprovedByEmployeeID = model.ApprovedByEmployeeID; entity.OrderDate = model.OrderDate; entity.ExpectedDeliveryDate = model.ExpectedDeliveryDate; entity.ActualDeliveryDate = model.ActualDeliveryDate; entity.SubTotal = model.SubTotal; entity.TaxAmount = model.TaxAmount; entity.DiscountAmount = model.DiscountAmount; entity.TotalAmount = model.TotalAmount; entity.PaymentStatus = model.PaymentStatus; entity.DeliveryStatus = model.DeliveryStatus; entity.Notes = model.Notes; entity.Status = model.Status; entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase order '{model.OrderNumber}' updated successfully."; return RedirectToAction(nameof(PurchaseOrders));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseOrders_Delete(int id) {
        var entity = await _context.PurchaseOrders.FindAsync(id); if (entity == null) return NotFound();
        _context.PurchaseOrders.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase order '{entity.OrderNumber}' deleted successfully."; return RedirectToAction(nameof(PurchaseOrders));
    }
    #endregion

    #region PurchaseReceipts
    public async Task<IActionResult> PurchaseReceipts() {
        var items = await _context.PurchaseReceipts.Include(r => r.Supplier).Include(r => r.PurchaseOrder).AsNoTracking().OrderByDescending(r => r.ReceiptDate)
            .Select(r => new PurchaseReceiptListVM { ReceiptID = r.ReceiptID, ReceiptNumber = r.ReceiptNumber, SupplierName = r.Supplier != null ? r.Supplier.SupplierName : null, PurchaseOrderNumber = r.PurchaseOrder != null ? r.PurchaseOrder.OrderNumber : null, TotalAmount = r.TotalAmount, Status = r.Status, ReceiptDate = r.ReceiptDate })
            .ToListAsync();
        return View("PurchaseReceipts/Index", items);
    }
    public async Task<IActionResult> PurchaseReceipts_Create() {
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").OrderByDescending(p => p.OrderDate).ToListAsync();
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        return View("PurchaseReceipts/Create", new PurchaseReceiptCreateVM());
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseReceipts_Create(PurchaseReceiptCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("PurchaseReceipts/Create", model); }
        _context.PurchaseReceipts.Add(new PurchaseReceipt { ReceiptNumber = model.ReceiptNumber, PurchaseOrderID = model.PurchaseOrderID, SupplierID = model.SupplierID, WarehouseID = model.WarehouseID, BranchID = model.BranchID, ReceivedByEmployeeID = model.ReceivedByEmployeeID, ReceiptDate = model.ReceiptDate, TotalAmount = model.TotalAmount, Status = model.Status, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase receipt '{model.ReceiptNumber}' created successfully."; return RedirectToAction(nameof(PurchaseReceipts));
    }
    public async Task<IActionResult> PurchaseReceipts_Edit(int id) {
        var entity = await _context.PurchaseReceipts.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        return View("PurchaseReceipts/Edit", new PurchaseReceiptEditVM { ReceiptID = entity.ReceiptID, ReceiptNumber = entity.ReceiptNumber, PurchaseOrderID = entity.PurchaseOrderID, SupplierID = entity.SupplierID, WarehouseID = entity.WarehouseID, BranchID = entity.BranchID, ReceivedByEmployeeID = entity.ReceivedByEmployeeID, ReceiptDate = entity.ReceiptDate, TotalAmount = entity.TotalAmount, Status = entity.Status, Notes = entity.Notes, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseReceipts_Edit(PurchaseReceiptEditVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("PurchaseReceipts/Edit", model); }
        var entity = await _context.PurchaseReceipts.FindAsync(model.ReceiptID); if (entity == null) return NotFound();
        entity.ReceiptNumber = model.ReceiptNumber; entity.PurchaseOrderID = model.PurchaseOrderID; entity.SupplierID = model.SupplierID; entity.WarehouseID = model.WarehouseID; entity.BranchID = model.BranchID; entity.ReceivedByEmployeeID = model.ReceivedByEmployeeID; entity.ReceiptDate = model.ReceiptDate; entity.TotalAmount = model.TotalAmount; entity.Status = model.Status; entity.Notes = model.Notes; entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase receipt '{model.ReceiptNumber}' updated successfully."; return RedirectToAction(nameof(PurchaseReceipts));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseReceipts_Delete(int id) {
        var entity = await _context.PurchaseReceipts.FindAsync(id); if (entity == null) return NotFound();
        _context.PurchaseReceipts.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase receipt '{entity.ReceiptNumber}' deleted successfully."; return RedirectToAction(nameof(PurchaseReceipts));
    }
    #endregion

    #region PurchaseInvoices
    public async Task<IActionResult> PurchaseInvoices() {
        var items = await _context.PurchaseInvoices.Include(p => p.Supplier).AsNoTracking().OrderByDescending(p => p.InvoiceDate)
            .Select(p => new PurchaseInvoiceListVM { InvoiceID = p.InvoiceID, InvoiceNumber = p.InvoiceNumber, SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null, TotalAmount = p.TotalAmount, PaymentStatus = p.PaymentStatus, Status = p.Status, InvoiceDate = p.InvoiceDate })
            .ToListAsync();
        return View("PurchaseInvoices/Index", items);
    }
    public async Task<IActionResult> PurchaseInvoices_Create() {
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").OrderByDescending(p => p.OrderDate).ToListAsync();
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.WarehouseName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        return View("PurchaseInvoices/Create", new PurchaseInvoiceCreateVM());
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseInvoices_Create(PurchaseInvoiceCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("PurchaseInvoices/Create", model); }
        _context.PurchaseInvoices.Add(new PurchaseInvoice { InvoiceNumber = model.InvoiceNumber, PurchaseOrderID = model.PurchaseOrderID, SupplierID = model.SupplierID, WarehouseID = model.WarehouseID, BranchID = model.BranchID, InvoiceDate = model.InvoiceDate, DueDate = model.DueDate, SubTotal = model.SubTotal, TaxAmount = model.TaxAmount, DiscountAmount = model.DiscountAmount, TotalAmount = model.TotalAmount, AmountPaid = model.AmountPaid, AmountDue = model.AmountDue, PaymentStatus = model.PaymentStatus, Status = model.Status, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase invoice '{model.InvoiceNumber}' created successfully."; return RedirectToAction(nameof(PurchaseInvoices));
    }
    public async Task<IActionResult> PurchaseInvoices_Edit(int id) {
        var entity = await _context.PurchaseInvoices.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        return View("PurchaseInvoices/Edit", new PurchaseInvoiceEditVM { InvoiceID = entity.InvoiceID, InvoiceNumber = entity.InvoiceNumber, PurchaseOrderID = entity.PurchaseOrderID, SupplierID = entity.SupplierID, WarehouseID = entity.WarehouseID, BranchID = entity.BranchID, InvoiceDate = entity.InvoiceDate, DueDate = entity.DueDate, SubTotal = entity.SubTotal, TaxAmount = entity.TaxAmount, DiscountAmount = entity.DiscountAmount, TotalAmount = entity.TotalAmount, AmountPaid = entity.AmountPaid, AmountDue = entity.AmountDue, PaymentStatus = entity.PaymentStatus, Status = entity.Status, Notes = entity.Notes, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseInvoices_Edit(PurchaseInvoiceEditVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("PurchaseInvoices/Edit", model); }
        var entity = await _context.PurchaseInvoices.FindAsync(model.InvoiceID); if (entity == null) return NotFound();
        entity.InvoiceNumber = model.InvoiceNumber; entity.PurchaseOrderID = model.PurchaseOrderID; entity.SupplierID = model.SupplierID; entity.WarehouseID = model.WarehouseID; entity.BranchID = model.BranchID; entity.InvoiceDate = model.InvoiceDate; entity.DueDate = model.DueDate; entity.SubTotal = model.SubTotal; entity.TaxAmount = model.TaxAmount; entity.DiscountAmount = model.DiscountAmount; entity.TotalAmount = model.TotalAmount; entity.AmountPaid = model.AmountPaid; entity.AmountDue = model.AmountDue; entity.PaymentStatus = model.PaymentStatus; entity.Status = model.Status; entity.Notes = model.Notes; entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase invoice '{model.InvoiceNumber}' updated successfully."; return RedirectToAction(nameof(PurchaseInvoices));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PurchaseInvoices_Delete(int id) {
        var entity = await _context.PurchaseInvoices.FindAsync(id); if (entity == null) return NotFound();
        _context.PurchaseInvoices.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Purchase invoice '{entity.InvoiceNumber}' deleted successfully."; return RedirectToAction(nameof(PurchaseInvoices));
    }
    #endregion

    #region SupplierPayments
    public async Task<IActionResult> SupplierPayments() {
        var items = await _context.SupplierPayments.Include(s => s.Supplier).AsNoTracking().OrderByDescending(s => s.PaymentDate)
            .Select(s => new SupplierPaymentListVM { PaymentID = s.PaymentID, PaymentNumber = s.PaymentNumber, SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null, Amount = s.Amount, PaymentMethod = s.PaymentMethod, PaymentDate = s.PaymentDate })
            .ToListAsync();
        return View("SupplierPayments/Index", items);
    }
    public async Task<IActionResult> SupplierPayments_Create() {
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").OrderByDescending(p => p.OrderDate).ToListAsync();
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
        return View("SupplierPayments/Create", new SupplierPaymentCreateVM());
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupplierPayments_Create(SupplierPaymentCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("SupplierPayments/Create", model); }
        _context.SupplierPayments.Add(new SupplierPayment { PaymentNumber = model.PaymentNumber, PurchaseOrderID = model.PurchaseOrderID, ReceiptID = model.ReceiptID, SupplierID = model.SupplierID, BranchID = model.BranchID, ProcessedByEmployeeID = model.ProcessedByEmployeeID, PaymentDate = model.PaymentDate, Amount = model.Amount, PaymentMethod = model.PaymentMethod, ReferenceNumber = model.ReferenceNumber, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier payment '{model.PaymentNumber}' created successfully."; return RedirectToAction(nameof(SupplierPayments));
    }
    public async Task<IActionResult> SupplierPayments_Edit(int id) {
        var entity = await _context.SupplierPayments.FindAsync(id); if (entity == null) return NotFound();
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Receipts = await _context.PurchaseReceipts.Where(r => r.Status != "Cancelled").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        return View("SupplierPayments/Edit", new SupplierPaymentEditVM { PaymentID = entity.PaymentID, PaymentNumber = entity.PaymentNumber, PurchaseOrderID = entity.PurchaseOrderID, ReceiptID = entity.ReceiptID, SupplierID = entity.SupplierID, BranchID = entity.BranchID, ProcessedByEmployeeID = entity.ProcessedByEmployeeID, PaymentDate = entity.PaymentDate, Amount = entity.Amount, PaymentMethod = entity.PaymentMethod, ReferenceNumber = entity.ReferenceNumber, Notes = entity.Notes, CreatedAt = entity.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupplierPayments_Edit(SupplierPaymentEditVM model) {
        if (!ModelState.IsValid) { ViewBag.PurchaseOrders = await _context.PurchaseOrders.Where(p => p.Status != "Cancelled" && p.Status != "Completed").ToListAsync(); ViewBag.Receipts = await _context.PurchaseReceipts.Where(r => r.Status != "Cancelled").ToListAsync(); ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("SupplierPayments/Edit", model); }
        var entity = await _context.SupplierPayments.FindAsync(model.PaymentID); if (entity == null) return NotFound();
        entity.PaymentNumber = model.PaymentNumber; entity.PurchaseOrderID = model.PurchaseOrderID; entity.ReceiptID = model.ReceiptID; entity.SupplierID = model.SupplierID; entity.BranchID = model.BranchID; entity.ProcessedByEmployeeID = model.ProcessedByEmployeeID; entity.PaymentDate = model.PaymentDate; entity.Amount = model.Amount; entity.PaymentMethod = model.PaymentMethod; entity.ReferenceNumber = model.ReferenceNumber; entity.Notes = model.Notes;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier payment '{model.PaymentNumber}' updated successfully."; return RedirectToAction(nameof(SupplierPayments));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> SupplierPayments_Delete(int id) {
        var entity = await _context.SupplierPayments.FindAsync(id); if (entity == null) return NotFound();
        _context.SupplierPayments.Remove(entity); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = $"Supplier payment '{entity.PaymentNumber}' deleted successfully."; return RedirectToAction(nameof(SupplierPayments));
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
