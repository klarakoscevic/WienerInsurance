using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages.Account
{

    [Authorize(Roles = "Admin")]
    public class CreateUserModel : PageModel
    {
        private readonly UserRepository _repo;
        public CreateUserModel(UserRepository repo) => _repo = repo;

       
        [BindProperty] public UserInputViewModel Input { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _repo.GetAllRolesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _repo.GetUserByEmailAsync(Input.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Input.Email", "Postoji korisnik s ovom email adresom");
                Roles = await _repo.GetAllRolesAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                Roles = await _repo.GetAllRolesAsync();
                return Page();
            }

            var hasher = new PasswordHasher<string>();
            var hash = hasher.HashPassword(null, Input.Password);

            var user = new User
            {
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                PasswordHash = hash,
                RoleId = Input.RoleId
            };

            var id = await _repo.CreateUserAsync(user);

            return RedirectToPage("/Account/ShowUsers");
        }
    }
}
