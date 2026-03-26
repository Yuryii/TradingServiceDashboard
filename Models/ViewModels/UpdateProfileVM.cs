using System.ComponentModel.DataAnnotations;

namespace Dashboard.Models.ViewModels;

public class UpdateProfileVM
{
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    public string? UserName { get; set; }

    [Display(Name = "Employee ID")]
    public int? EmployeeID { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword))]
    public string? ConfirmPassword { get; set; }
}
