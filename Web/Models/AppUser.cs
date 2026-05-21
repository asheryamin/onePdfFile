using Microsoft.AspNetCore.Identity;

namespace onePdfFile.Web.Models;

public class AppUser : IdentityUser
{
    public bool IsFirstLogin { get; set; } = true;
    public Customer? Customer { get; set; }
}
