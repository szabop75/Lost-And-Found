using System.Threading.Tasks;
using LostAndFound.Infrastructure.Data;
using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public record MeResponse(string? Email, string? FullName);

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Fallback: try by name (email) from principal
            var email = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(email))
            {
                user = await _userManager.FindByEmailAsync(email);
            }
        }
        if (user == null) return Unauthorized();
        return Ok(new MeResponse(user.Email, user.FullName));
    }
}
