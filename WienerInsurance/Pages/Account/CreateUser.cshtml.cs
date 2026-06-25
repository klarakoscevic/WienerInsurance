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
        private readonly ILogger<CreateUserModel> _logger;

        public CreateUserModel(UserRepository repo, ILogger<CreateUserModel> logger)
        {
            _repo = repo;
            _logger = logger;
        }

       
        [BindProperty] public UserInputViewModel Input { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Roles = await _repo.GetAllRolesAsync() ?? Enumerable.Empty<UserRole>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles for create user page");
                TempData["Error"] = "Došlo je do greške prilikom učitavanja podataka. Molimo pokušajte ponovo.";
                Roles = new List<UserRole>();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var existing = await _repo.GetUserByEmailAsync(Input.Email);
                if (existing != null)
                {
                    ModelState.AddModelError("Input.Email", "Postoji korisnik s ovom email adresom");
                    Roles = await _repo.GetAllRolesAsync() ?? Enumerable.Empty<UserRole>();
                    return Page();
                }

                if (!ModelState.IsValid)
                {
                    Roles = await _repo.GetAllRolesAsync() ?? Enumerable.Empty<UserRole>();
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
                    RoleId = Input.RoleId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                // set CreatedByUserId from the currently logged in user (fallback to system user id 1)
                var current = await _repo.GetUserByEmailAsync(User?.Identity?.Name);
                user.CreatedByUserId = current?.Id ?? 1;

                var id = await _repo.CreateUserAsync(user);
                _logger.LogInformation("User created successfully. Email: {Email}, CreatedBy: {CreatedBy}", 
                    Input.Email, User?.Identity?.Name);

                return RedirectToPage("/Account/ShowUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user. Email: {Email}, CreatedBy: {CreatedBy}", 
                    Input?.Email, User?.Identity?.Name);
                ModelState.AddModelError("", "Došlo je do greške prilikom spremanja. Molimo pokušajte ponovo.");
                Roles = await _repo.GetAllRolesAsync() ?? Enumerable.Empty<UserRole>();
                return Page();
            }
        }
    }
}
