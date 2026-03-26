using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Models.ViewModels;
using Dashboard.Services;

namespace Dashboard.Controllers
{

[Authorize(Policy = "HRPolicy")]
public class HumanResourcesCrudController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelCrudService _excelService;

    public HumanResourcesCrudController(ApplicationDbContext context, ExcelCrudService excelService)
    {
        _context = context;
        _excelService = excelService;
    }

    public IActionResult Index() => View();

    #region Positions
    public async Task<IActionResult> Positions() {
        var items = await _context.Positions.AsNoTracking().OrderBy(p => p.PositionName)
            .Select(p => new PositionListVM { PositionID = p.PositionID, PositionCode = p.PositionCode, PositionName = p.PositionName, PositionLevel = p.PositionLevel, Description = p.Description, IsActive = p.IsActive })
            .ToListAsync();
        return View("Positions/Index", items);
    }
    public IActionResult Positions_Create() => View("Positions/Create");
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Positions_Create(PositionCreateVM model) {
        if (!ModelState.IsValid) return View("Positions/Create", model);
        _context.Positions.Add(new Position { PositionCode = model.PositionCode, PositionName = model.PositionName, PositionLevel = model.PositionLevel, Description = model.Description, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Position created successfully."; return RedirectToAction(nameof(Positions));
    }
    public async Task<IActionResult> Positions_Edit(int id) {
        var e = await _context.Positions.FindAsync(id); if (e == null) return NotFound();
        return View("Positions/Edit", new PositionEditVM { PositionID = e.PositionID, PositionCode = e.PositionCode, PositionName = e.PositionName, PositionLevel = e.PositionLevel, Description = e.Description, IsActive = e.IsActive, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Positions_Edit(PositionEditVM model) {
        if (!ModelState.IsValid) return View("Positions/Edit", model);
        var e = await _context.Positions.FindAsync(model.PositionID); if (e == null) return NotFound();
        e.PositionCode = model.PositionCode; e.PositionName = model.PositionName; e.PositionLevel = model.PositionLevel; e.Description = model.Description; e.IsActive = model.IsActive;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Position updated successfully."; return RedirectToAction(nameof(Positions));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Positions_Delete(int id) {
        var e = await _context.Positions.FindAsync(id); if (e == null) return NotFound();
        _context.Positions.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Position deleted successfully."; return RedirectToAction(nameof(Positions));
    }
    #endregion

    #region Employees
    public async Task<IActionResult> Employees() {
        var items = await _context.Employees.Include(e => e.Department).Include(e => e.Position).AsNoTracking().OrderBy(e => e.FullName)
            .Select(e => new EmployeeListVM { EmployeeID = e.EmployeeID, EmployeeCode = e.EmployeeCode, FullName = e.FullName, DepartmentName = e.Department != null ? e.Department.DepartmentName : null, PositionName = e.Position != null ? e.Position.PositionName : null, Phone = e.Phone, Email = e.Email, EmploymentType = e.EmploymentType, IsActive = e.IsActive })
            .ToListAsync();
        return View("Employees/Index", items);
    }
    public async Task<IActionResult> Employees_Create() {
        ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToListAsync();
        ViewBag.Positions = await _context.Positions.Where(p => p.IsActive).OrderBy(p => p.PositionName).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
        ViewBag.Managers = await _context.Employees.Where(m => m.IsActive).OrderBy(m => m.FullName).ToListAsync();
        return View("Employees/Create");
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Employees_Create(EmployeeCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Positions = await _context.Positions.Where(p => p.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Managers = await _context.Employees.Where(m => m.IsActive).ToListAsync(); return View("Employees/Create", model); }
        _context.Employees.Add(new Employee { EmployeeCode = model.EmployeeCode, FullName = model.FullName, Gender = model.Gender, DateOfBirth = model.DateOfBirth, Phone = model.Phone, Email = model.Email, AddressLine = model.AddressLine, DepartmentID = model.DepartmentID, PositionID = model.PositionID, BranchID = model.BranchID, ManagerID = model.ManagerID, EmploymentType = model.EmploymentType, HireDate = model.HireDate, BaseSalary = model.BaseSalary, IsActive = model.IsActive, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Employee created successfully."; return RedirectToAction(nameof(Employees));
    }
    public async Task<IActionResult> Employees_Edit(int id) {
        var e = await _context.Employees.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Positions = await _context.Positions.Where(p => p.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Managers = await _context.Employees.Where(m => m.IsActive).ToListAsync();
        return View("Employees/Edit", new EmployeeEditVM { EmployeeID = e.EmployeeID, EmployeeCode = e.EmployeeCode, FullName = e.FullName, Gender = e.Gender, DateOfBirth = e.DateOfBirth, Phone = e.Phone, Email = e.Email, AddressLine = e.AddressLine, DepartmentID = e.DepartmentID, PositionID = e.PositionID, BranchID = e.BranchID, ManagerID = e.ManagerID, EmploymentType = e.EmploymentType, HireDate = e.HireDate, TerminationDate = e.TerminationDate, TerminationReason = e.TerminationReason, BaseSalary = e.BaseSalary, IsActive = e.IsActive, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Employees_Edit(EmployeeEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Positions = await _context.Positions.Where(p => p.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Managers = await _context.Employees.Where(m => m.IsActive).ToListAsync(); return View("Employees/Edit", model); }
        var e = await _context.Employees.FindAsync(model.EmployeeID); if (e == null) return NotFound();
        e.EmployeeCode = model.EmployeeCode; e.FullName = model.FullName; e.Gender = model.Gender; e.DateOfBirth = model.DateOfBirth; e.Phone = model.Phone; e.Email = model.Email; e.AddressLine = model.AddressLine; e.DepartmentID = model.DepartmentID; e.PositionID = model.PositionID; e.BranchID = model.BranchID; e.ManagerID = model.ManagerID; e.EmploymentType = model.EmploymentType; e.HireDate = model.HireDate; e.TerminationDate = model.TerminationDate; e.TerminationReason = model.TerminationReason; e.BaseSalary = model.BaseSalary; e.IsActive = model.IsActive; e.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Employee updated successfully."; return RedirectToAction(nameof(Employees));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Employees_Delete(int id) {
        var e = await _context.Employees.FindAsync(id); if (e == null) return NotFound();
        _context.Employees.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Employee deleted successfully."; return RedirectToAction(nameof(Employees));
    }
    #endregion

    #region Attendances
    public async Task<IActionResult> Attendances() {
        var items = await _context.Attendances.Include(a => a.Employee).AsNoTracking().OrderByDescending(a => a.AttendanceDate)
            .Select(a => new AttendanceListVM { AttendanceID = a.AttendanceID, EmployeeName = a.Employee != null ? a.Employee.FullName : null, AttendanceDate = a.AttendanceDate, CheckInTime = a.CheckInTime, CheckOutTime = a.CheckOutTime, Status = a.Status, Notes = a.Notes })
            .ToListAsync();
        return View("Attendances/Index", items);
    }
    public async Task<IActionResult> Attendances_Create() { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); return View("Attendances/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Attendances_Create(AttendanceCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("Attendances/Create", model); }
        _context.Attendances.Add(new Attendance { EmployeeID = model.EmployeeID, AttendanceDate = model.AttendanceDate, CheckInTime = model.CheckInTime, CheckOutTime = model.CheckOutTime, Status = model.Status, Notes = model.Notes });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Attendance created successfully."; return RedirectToAction(nameof(Attendances));
    }
    public async Task<IActionResult> Attendances_Edit(int id) {
        var e = await _context.Attendances.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync();
        return View("Attendances/Edit", new AttendanceEditVM { AttendanceID = e.AttendanceID, EmployeeID = e.EmployeeID, AttendanceDate = e.AttendanceDate, CheckInTime = e.CheckInTime, CheckOutTime = e.CheckOutTime, Status = e.Status, Notes = e.Notes });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Attendances_Edit(AttendanceEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); return View("Attendances/Edit", model); }
        var e = await _context.Attendances.FindAsync(model.AttendanceID); if (e == null) return NotFound();
        e.EmployeeID = model.EmployeeID; e.AttendanceDate = model.AttendanceDate; e.CheckInTime = model.CheckInTime; e.CheckOutTime = model.CheckOutTime; e.Status = model.Status; e.Notes = model.Notes;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Attendance updated successfully."; return RedirectToAction(nameof(Attendances));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Attendances_Delete(int id) {
        var e = await _context.Attendances.FindAsync(id); if (e == null) return NotFound();
        _context.Attendances.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Attendance deleted successfully."; return RedirectToAction(nameof(Attendances));
    }
    #endregion

    #region LeaveRequests
    public async Task<IActionResult> LeaveRequests() {
        var items = await _context.LeaveRequests.Include(l => l.Employee).AsNoTracking().OrderByDescending(l => l.CreatedAt)
            .Select(l => new LeaveRequestListVM { LeaveRequestID = l.LeaveRequestID, EmployeeName = l.Employee != null ? l.Employee.FullName : null, LeaveType = l.LeaveType, StartDate = l.StartDate, EndDate = l.EndDate, TotalDays = l.TotalDays, Status = l.Status })
            .ToListAsync();
        return View("LeaveRequests/Index", items);
    }
    public async Task<IActionResult> LeaveRequests_Create() { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); return View("LeaveRequests/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> LeaveRequests_Create(LeaveRequestCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("LeaveRequests/Create", model); }
        _context.LeaveRequests.Add(new LeaveRequest { EmployeeID = model.EmployeeID, LeaveType = model.LeaveType, StartDate = model.StartDate, EndDate = model.EndDate, TotalDays = model.TotalDays, Reason = model.Reason, Status = model.Status, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Leave request created successfully."; return RedirectToAction(nameof(LeaveRequests));
    }
    public async Task<IActionResult> LeaveRequests_Edit(int id) {
        var e = await _context.LeaveRequests.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync();
        return View("LeaveRequests/Edit", new LeaveRequestEditVM { LeaveRequestID = e.LeaveRequestID, EmployeeID = e.EmployeeID, LeaveType = e.LeaveType, StartDate = e.StartDate, EndDate = e.EndDate, TotalDays = e.TotalDays, Reason = e.Reason, Status = e.Status, ApprovedByEmployeeID = e.ApprovedByEmployeeID, ApprovedDate = e.ApprovedDate, RejectionReason = e.RejectionReason, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> LeaveRequests_Edit(LeaveRequestEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); return View("LeaveRequests/Edit", model); }
        var e = await _context.LeaveRequests.FindAsync(model.LeaveRequestID); if (e == null) return NotFound();
        e.EmployeeID = model.EmployeeID; e.LeaveType = model.LeaveType; e.StartDate = model.StartDate; e.EndDate = model.EndDate; e.TotalDays = model.TotalDays; e.Reason = model.Reason; e.Status = model.Status; e.ApprovedByEmployeeID = model.ApprovedByEmployeeID; e.ApprovedDate = model.ApprovedDate; e.RejectionReason = model.RejectionReason;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Leave request updated successfully."; return RedirectToAction(nameof(LeaveRequests));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> LeaveRequests_Delete(int id) {
        var e = await _context.LeaveRequests.FindAsync(id); if (e == null) return NotFound();
        _context.LeaveRequests.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Leave request deleted successfully."; return RedirectToAction(nameof(LeaveRequests));
    }
    #endregion

    #region Payrolls
    public async Task<IActionResult> Payrolls() {
        var items = await _context.Payrolls.Include(p => p.Employee).AsNoTracking().OrderByDescending(p => p.PaymentDate)
            .Select(p => new PayrollListVM { PayrollID = p.PayrollID, EmployeeName = p.Employee != null ? p.Employee.FullName : null, PayrollPeriod = p.PayrollPeriod, PaymentDate = p.PaymentDate, NetSalary = p.NetSalary, Status = p.Status })
            .ToListAsync();
        return View("Payrolls/Index", items);
    }
    public async Task<IActionResult> Payrolls_Create() { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync(); return View("Payrolls/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Payrolls_Create(PayrollCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("Payrolls/Create", model); }
        _context.Payrolls.Add(new Payroll { EmployeeID = model.EmployeeID, BranchID = model.BranchID, PayrollPeriod = model.PayrollPeriod, PeriodStartDate = model.PeriodStartDate, PeriodEndDate = model.PeriodEndDate, PaymentDate = model.PaymentDate, BaseSalary = model.BaseSalary, OvertimeAmount = model.OvertimeAmount, BonusAmount = model.BonusAmount, AllowanceAmount = model.AllowanceAmount, DeductionAmount = model.DeductionAmount, TaxAmount = model.TaxAmount, NetSalary = model.NetSalary, Status = model.Status, Notes = model.Notes, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Payroll created successfully."; return RedirectToAction(nameof(Payrolls));
    }
    public async Task<IActionResult> Payrolls_Edit(int id) {
        var e = await _context.Payrolls.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
        return View("Payrolls/Edit", new PayrollEditVM { PayrollID = e.PayrollID, EmployeeID = e.EmployeeID, BranchID = e.BranchID, PayrollPeriod = e.PayrollPeriod, PeriodStartDate = e.PeriodStartDate, PeriodEndDate = e.PeriodEndDate, PaymentDate = e.PaymentDate, BaseSalary = e.BaseSalary, OvertimeAmount = e.OvertimeAmount, BonusAmount = e.BonusAmount, AllowanceAmount = e.AllowanceAmount, DeductionAmount = e.DeductionAmount, TaxAmount = e.TaxAmount, NetSalary = e.NetSalary, Status = e.Status, Notes = e.Notes, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Payrolls_Edit(PayrollEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); return View("Payrolls/Edit", model); }
        var e = await _context.Payrolls.FindAsync(model.PayrollID); if (e == null) return NotFound();
        e.EmployeeID = model.EmployeeID; e.BranchID = model.BranchID; e.PayrollPeriod = model.PayrollPeriod; e.PeriodStartDate = model.PeriodStartDate; e.PeriodEndDate = model.PeriodEndDate; e.PaymentDate = model.PaymentDate; e.BaseSalary = model.BaseSalary; e.OvertimeAmount = model.OvertimeAmount; e.BonusAmount = model.BonusAmount; e.AllowanceAmount = model.AllowanceAmount; e.DeductionAmount = model.DeductionAmount; e.TaxAmount = model.TaxAmount; e.NetSalary = model.NetSalary; e.Status = model.Status; e.Notes = model.Notes;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Payroll updated successfully."; return RedirectToAction(nameof(Payrolls));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Payrolls_Delete(int id) {
        var e = await _context.Payrolls.FindAsync(id); if (e == null) return NotFound();
        _context.Payrolls.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Payroll deleted successfully."; return RedirectToAction(nameof(Payrolls));
    }
    #endregion

    #region PerformanceReviews
    public async Task<IActionResult> PerformanceReviews() {
        var items = await _context.PerformanceReviews.Include(r => r.Employee).Include(r => r.ReviewedByEmployee).AsNoTracking().OrderByDescending(r => r.ReviewDate)
            .Select(r => new PerformanceReviewListVM { ReviewID = r.ReviewID, EmployeeName = r.Employee != null ? r.Employee.FullName : null, ReviewedByName = r.ReviewedByEmployee != null ? r.ReviewedByEmployee.FullName : null, ReviewDate = r.ReviewDate, OverallRating = r.OverallRating })
            .ToListAsync();
        return View("PerformanceReviews/Index", items);
    }
    public async Task<IActionResult> PerformanceReviews_Create() { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); return View("PerformanceReviews/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PerformanceReviews_Create(PerformanceReviewCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("PerformanceReviews/Create", model); }
        _context.PerformanceReviews.Add(new PerformanceReview { EmployeeID = model.EmployeeID, ReviewedByEmployeeID = model.ReviewedByEmployeeID, ReviewDate = model.ReviewDate, ReviewPeriodStart = model.ReviewPeriodStart, ReviewPeriodEnd = model.ReviewPeriodEnd, OverallRating = model.OverallRating, Strengths = model.Strengths, AreasForImprovement = model.AreasForImprovement, Comments = model.Comments, Goals = model.Goals, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Performance review created successfully."; return RedirectToAction(nameof(PerformanceReviews));
    }
    public async Task<IActionResult> PerformanceReviews_Edit(int id) {
        var e = await _context.PerformanceReviews.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync();
        return View("PerformanceReviews/Edit", new PerformanceReviewEditVM { ReviewID = e.ReviewID, EmployeeID = e.EmployeeID, ReviewedByEmployeeID = e.ReviewedByEmployeeID, ReviewDate = e.ReviewDate, ReviewPeriodStart = e.ReviewPeriodStart, ReviewPeriodEnd = e.ReviewPeriodEnd, OverallRating = e.OverallRating, Strengths = e.Strengths, AreasForImprovement = e.AreasForImprovement, Comments = e.Comments, Goals = e.Goals, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PerformanceReviews_Edit(PerformanceReviewEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); return View("PerformanceReviews/Edit", model); }
        var e = await _context.PerformanceReviews.FindAsync(model.ReviewID); if (e == null) return NotFound();
        e.EmployeeID = model.EmployeeID; e.ReviewedByEmployeeID = model.ReviewedByEmployeeID; e.ReviewDate = model.ReviewDate; e.ReviewPeriodStart = model.ReviewPeriodStart; e.ReviewPeriodEnd = model.ReviewPeriodEnd; e.OverallRating = model.OverallRating; e.Strengths = model.Strengths; e.AreasForImprovement = model.AreasForImprovement; e.Comments = model.Comments; e.Goals = model.Goals;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Performance review updated successfully."; return RedirectToAction(nameof(PerformanceReviews));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> PerformanceReviews_Delete(int id) {
        var e = await _context.PerformanceReviews.FindAsync(id); if (e == null) return NotFound();
        _context.PerformanceReviews.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Performance review deleted successfully."; return RedirectToAction(nameof(PerformanceReviews));
    }
    #endregion

    #region JobOpenings
    public async Task<IActionResult> JobOpenings() {
        var items = await _context.JobOpenings.Include(j => j.Department).AsNoTracking().OrderByDescending(j => j.PostedDate)
            .Select(j => new JobOpeningListVM { JobOpeningID = j.JobOpeningID, Title = j.Title, DepartmentName = j.Department != null ? j.Department.DepartmentName : null, EmploymentType = j.EmploymentType, Location = j.Location, NumberOfPositions = j.NumberOfPositions, Status = j.Status, PostedDate = j.PostedDate })
            .ToListAsync();
        return View("JobOpenings/Index", items);
    }
    public async Task<IActionResult> JobOpenings_Create() { ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).OrderBy(b => b.BranchName).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); return View("JobOpenings/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> JobOpenings_Create(JobOpeningCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("JobOpenings/Create", model); }
        _context.JobOpenings.Add(new JobOpening { Title = model.Title, DepartmentID = model.DepartmentID, BranchID = model.BranchID, EmploymentType = model.EmploymentType, Location = model.Location, SalaryMin = model.SalaryMin, SalaryMax = model.SalaryMax, NumberOfPositions = model.NumberOfPositions, JobDescription = model.JobDescription, Requirements = model.Requirements, PostedDate = model.PostedDate, ClosingDate = model.ClosingDate, Status = model.Status, CreatedByEmployeeID = model.CreatedByEmployeeID, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Job opening created successfully."; return RedirectToAction(nameof(JobOpenings));
    }
    public async Task<IActionResult> JobOpenings_Edit(int id) {
        var e = await _context.JobOpenings.FindAsync(id); if (e == null) return NotFound();
        ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync();
        return View("JobOpenings/Edit", new JobOpeningEditVM { JobOpeningID = e.JobOpeningID, Title = e.Title, DepartmentID = e.DepartmentID, BranchID = e.BranchID, EmploymentType = e.EmploymentType, Location = e.Location, SalaryMin = e.SalaryMin, SalaryMax = e.SalaryMax, NumberOfPositions = e.NumberOfPositions, JobDescription = e.JobDescription, Requirements = e.Requirements, PostedDate = e.PostedDate, ClosingDate = e.ClosingDate, Status = e.Status, CreatedAt = e.CreatedAt });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> JobOpenings_Edit(JobOpeningEditVM model) {
        if (!ModelState.IsValid) { ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(); ViewBag.Branches = await _context.Branches.Where(b => b.IsActive).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); return View("JobOpenings/Edit", model); }
        var e = await _context.JobOpenings.FindAsync(model.JobOpeningID); if (e == null) return NotFound();
        e.Title = model.Title; e.DepartmentID = model.DepartmentID; e.BranchID = model.BranchID; e.EmploymentType = model.EmploymentType; e.Location = model.Location; e.SalaryMin = model.SalaryMin; e.SalaryMax = model.SalaryMax; e.NumberOfPositions = model.NumberOfPositions; e.JobDescription = model.JobDescription; e.Requirements = model.Requirements; e.PostedDate = model.PostedDate; e.ClosingDate = model.ClosingDate; e.Status = model.Status;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Job opening updated successfully."; return RedirectToAction(nameof(JobOpenings));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> JobOpenings_Delete(int id) {
        var e = await _context.JobOpenings.FindAsync(id); if (e == null) return NotFound();
        _context.JobOpenings.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Job opening deleted successfully."; return RedirectToAction(nameof(JobOpenings));
    }
    #endregion

    #region Applicants
    public async Task<IActionResult> Applicants() {
        var items = await _context.Applicants.Include(a => a.JobOpening).AsNoTracking().OrderByDescending(a => a.AppliedDate)
            .Select(a => new ApplicantListVM { ApplicantID = a.ApplicantID, FullName = a.FullName, Email = a.Email, Phone = a.Phone, JobOpeningTitle = a.JobOpening != null ? a.JobOpening.Title : null, Status = a.Status, AppliedDate = a.AppliedDate })
            .ToListAsync();
        return View("Applicants/Index", items);
    }
    public async Task<IActionResult> Applicants_Create() { ViewBag.JobOpenings = await _context.JobOpenings.Where(j => j.Status == "Open").OrderByDescending(j => j.PostedDate).ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(); return View("Applicants/Create"); }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Applicants_Create(ApplicantCreateVM model) {
        if (!ModelState.IsValid) { ViewBag.JobOpenings = await _context.JobOpenings.Where(j => j.Status == "Open").ToListAsync(); ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(); return View("Applicants/Create", model); }
        _context.Applicants.Add(new Applicant { JobOpeningID = model.JobOpeningID, FullName = model.FullName, Gender = model.Gender, DateOfBirth = model.DateOfBirth, Phone = model.Phone, Email = model.Email, Address = model.Address, ResumePath = model.ResumePath, CoverLetter = model.CoverLetter, LinkedInProfile = model.LinkedInProfile, Status = model.Status, ReferredByEmployeeID = model.ReferredByEmployeeID, AppliedDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Applicant created successfully."; return RedirectToAction(nameof(Applicants));
    }
    public async Task<IActionResult> Applicants_Edit(int id) {
        var e = await _context.Applicants.FindAsync(id); if (e == null) return NotFound();
        ViewBag.JobOpenings = await _context.JobOpenings.ToListAsync(); ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync();
        return View("Applicants/Edit", new ApplicantEditVM { ApplicantID = e.ApplicantID, JobOpeningID = e.JobOpeningID, FullName = e.FullName, Gender = e.Gender, DateOfBirth = e.DateOfBirth, Phone = e.Phone, Email = e.Email, Address = e.Address, ResumePath = e.ResumePath, CoverLetter = e.CoverLetter, LinkedInProfile = e.LinkedInProfile, Status = e.Status, InterviewDate = e.InterviewDate, Notes = e.Notes, AppliedDate = e.AppliedDate });
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Applicants_Edit(ApplicantEditVM model) {
        if (!ModelState.IsValid) { ViewBag.JobOpenings = await _context.JobOpenings.ToListAsync(); ViewBag.Employees = await _context.Employees.Where(emp => emp.IsActive).ToListAsync(); return View("Applicants/Edit", model); }
        var e = await _context.Applicants.FindAsync(model.ApplicantID); if (e == null) return NotFound();
        e.JobOpeningID = model.JobOpeningID; e.FullName = model.FullName; e.Gender = model.Gender; e.DateOfBirth = model.DateOfBirth; e.Phone = model.Phone; e.Email = model.Email; e.Address = model.Address; e.ResumePath = model.ResumePath; e.CoverLetter = model.CoverLetter; e.LinkedInProfile = model.LinkedInProfile; e.Status = model.Status; e.InterviewDate = model.InterviewDate; e.Notes = model.Notes;
        await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Applicant updated successfully."; return RedirectToAction(nameof(Applicants));
    }
    [HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Applicants_Delete(int id) {
        var e = await _context.Applicants.FindAsync(id); if (e == null) return NotFound();
        _context.Applicants.Remove(e); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Applicant deleted successfully."; return RedirectToAction(nameof(Applicants));
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

    public class HRCrudIndexVM
    {
    }
}