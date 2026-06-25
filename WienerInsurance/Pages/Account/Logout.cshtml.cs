using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var userName = User?.Identity?.Name;
            await HttpContext.SignOutAsync("MyCookieAuth");
            _logger.LogInformation("User {UserName} logged out successfully", userName);
            return RedirectToPage("/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserName}", User?.Identity?.Name);
            return RedirectToPage("/Account/Login");
        }
    }
}
