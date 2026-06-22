using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;

namespace WienerInsurance.Pages.Account
{

    [Authorize(Roles = "Admin")]
    public class CreateUserModel : PageModel
    {
        private readonly UserRepository _repo;
        public CreateUserModel(UserRepository repo) => _repo = repo;

        [BindProperty] public string Email { get; set; }
        [BindProperty] public string FirstName { get; set; }
        [BindProperty] public string LastName { get; set; }
        [BindProperty] public string Password { get; set; }
        [BindProperty] public int RoleId { get; set; }

        public IEnumerable<UserRole> Roles { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _repo.GetAllRolesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _repo.GetUserByEmailAsync(Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "User with this email already exists.");
                Roles = await _repo.GetAllRolesAsync();
                return Page();
            }

            var hasher = new PasswordHasher<string>();
            var hash = hasher.HashPassword(null, Password);

            var user = new User
            {
                Email = Email,
                FirstName = FirstName,
                LastName = LastName,
                PasswordHash = hash,
                RoleId = RoleId
            };

            var id = await _repo.CreateUserAsync(user);

            return RedirectToPage("/Account/ShowUsers");
        }
    }
}
