using Microsoft.AspNetCore.Identity;

namespace Dashboard.Models;

public class ApplicationUser : IdentityUser
{
    public int? EmployeeID { get; set; }
    
    public string? FullName { get; set; }
    
    public DateTime? CreatedAt { get; set; }
}
