using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Services;

namespace Dashboard.Controllers;

[Authorize]
public class ExcelCrudController : Controller
{
    private readonly ExcelCrudService _excelService;

    public ExcelCrudController(ExcelCrudService excelService)
    {
        _excelService = excelService;
    }

    // GET: Download template file for an entity
    public async Task<IActionResult> DownloadTemplate(string entityType, string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityName))
            return BadRequest("Invalid entity.");

        try
        {
            var bytes = await _excelService.GenerateTemplateAsync(entityType);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{entityName}_ImportTemplate.xlsx");
        }
        catch (Exception ex)
        {
            return Content($"Error generating template: {ex.Message}");
        }
    }

    // GET: Export all data for an entity to Excel
    public async Task<IActionResult> Export(string entityType, string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityName))
            return BadRequest("Invalid entity.");

        try
        {
            var bytes = await _excelService.ExportAsync(entityType);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{entityName}_Export_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            return Content($"Error exporting: {ex.Message}");
        }
    }

    // POST: Preview/Validate uploaded Excel file
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return Json(new { error = "Entity type is required." });

        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return Json(new { error = "Please select an Excel file." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return Json(new { error = "Only Excel files (.xlsx, .xls) are supported." });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var readResult = await _excelService.ReadExcelAsync(stream);
        if (!readResult.Success)
            return Json(new { error = readResult.Error });

        if (readResult.Rows.Count == 0)
            return Json(new { error = "No data rows found in the file. Make sure data starts from row 2." });

        var validationResult = await _excelService.ValidateAsync(readResult.Rows, entityType);

        return Json(new
        {
            success = validationResult.Success,
            error = validationResult.Error,
            totalRows = validationResult.TotalRows,
            invalidRows = validationResult.InvalidRows,
            errors = validationResult.Errors.Take(20).ToList(), // limit errors shown
            headers = readResult.Headers,
            previewRows = readResult.Rows.Take(10).ToList() // first 10 rows as preview
        });
    }

    // POST: Actually import the validated data
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return Json(new { success = false, message = "Entity type is required." });

        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "Please select an Excel file." });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var readResult = await _excelService.ReadExcelAsync(stream);
        if (!readResult.Success)
            return Json(new { success = false, message = readResult.Error });

        var validationResult = await _excelService.ValidateAsync(readResult.Rows, entityType);
        if (!validationResult.Success && validationResult.ValidRows.Count == 0)
            return Json(new { success = false, message = $"Validation failed: {string.Join("; ", validationResult.Errors.Take(5))}" });

        var importResult = await _excelService.ImportAsync(validationResult.ValidRows, entityType);

        if (importResult.Success)
            return Json(new
            {
                success = true,
                message = $"Successfully imported {importResult.SuccessCount} record(s)."
            });

        return Json(new
        {
            success = false,
            message = $"Imported {importResult.SuccessCount} record(s), failed {importResult.FailCount}. " +
                      string.Join("; ", importResult.Errors.Take(5))
        });
    }
}
