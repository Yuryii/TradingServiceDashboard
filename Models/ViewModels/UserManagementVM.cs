using System.ComponentModel.DataAnnotations;

namespace Dashboard.Models.ViewModels;

public class UserListVM
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    public string? UserName { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();
}

public class CreateUserVM
{
    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;
}

public class EditUserVM
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Current Roles")]
    public IList<string> CurrentRoles { get; set; } = new List<string>();

    [Required]
    [Display(Name = "Assign Role")]
    public string NewRole { get; set; } = string.Empty;
}
