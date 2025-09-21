using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Occupation { get; set; } // foglalkozás
}
