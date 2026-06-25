using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace WienerInsurance.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserRepository _repo;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(UserRepository repo, ILogger<LoginModel> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Password { get; set; }

        public IEnumerable<string> AdminEmails { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                AdminEmails = await _repo.GetAllAdminEmailsAsync() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin emails for login page");
                AdminEmails = new List<string>();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var user = await _repo.GetUserByEmailAsync(Email);
                var roles = await _repo.GetAllRolesAsync();


                var hasher = new PasswordHasher<string>();

                if (user != null && hasher.VerifyHashedPassword(null, user.PasswordHash, Password) == PasswordVerificationResult.Success)
                {
                    var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault(g => g.Id == user.RoleId)?.Name)
                };
                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(identity));
                    _logger.LogInformation("User {Email} logged in successfully", Email);
                    return RedirectToPage("/Index");
                }
                _logger.LogWarning("Failed login attempt for email: {Email}", Email);
                ModelState.AddModelError("", "Neispravni podaci.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for email: {Email}", Email);
                ModelState.AddModelError("", "Došlo je do greške prilikom prijave. Molimo pokušajte ponovo.");
                return Page();
            }
        }
    }
}