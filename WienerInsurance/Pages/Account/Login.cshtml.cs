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
        public LoginModel(UserRepository repo) => _repo = repo;

        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Password { get; set; }

        public IEnumerable<string> AdminEmails { get; set; }

        public async Task OnGetAsync()
        {
            AdminEmails = await _repo.GetAllAdminEmailsAsync() ?? new List<string>();
        }

        public async Task<IActionResult> OnPostAsync()
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
                return RedirectToPage("/Index");
            }
            ModelState.AddModelError("", "Neispravni podaci.");
            return Page();
        }
    }
}