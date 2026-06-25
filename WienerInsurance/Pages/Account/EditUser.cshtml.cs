using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages.Account
{
    [Authorize]
    public class EditUserModel : PageModel
    {
        private readonly UserRepository _repo;
        private readonly ILogger<EditUserModel> _logger;

        public EditUserModel(UserRepository repo, ILogger<EditUserModel> logger)
        {
            _repo = repo;
            _logger = logger;
        }
        [BindProperty] public EditUserViewModel Input { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            try
            {
                var user = id.HasValue ? await _repo.GetUserByIdAsync(id.Value) : await _repo.GetUserByEmailAsync(User.Identity.Name);

                if (user == null)
                {
                    return NotFound();
                }

                Input = new EditUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    OriginalEmail = user.Email,
                    RoleId = user.RoleId
                };

                Roles = await _repo.GetAllRolesAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for editing. UserId: {UserId}, RequestedBy: {RequestedBy}", 
                    id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom učitavanja podataka. Molimo pokušajte ponovo.";
                return RedirectToPage("/Account/ShowUsers");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Roles = await _repo.GetAllRolesAsync();

                if (!string.Equals(Input.Email, Input.OriginalEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _repo.GetUserByEmailAsync(Input.Email);
                    if (exists != null)
                    {
                        ModelState.AddModelError("Input.Email", "Postoji korisnik s ovom email adresom");
                        return Page();
                    }
                }

                if (!ModelState.IsValid)
                {
                    Roles = await _repo.GetAllRolesAsync();
                    var errors = ModelState.Values.SelectMany(v => v.Errors);

                    return Page();
                }

                var user = await _repo.GetUserByIdAsync(Input.Id);

                if (user == null) return NotFound();

                var current = await _repo.GetUserByEmailAsync(User.Identity.Name);
                if (current == null) return Forbid();
                if (user.Id != current.Id && !User.IsInRole("Admin")) return Forbid();


                string passwordHash = user.PasswordHash;
                if (!string.IsNullOrWhiteSpace(Input.Password))
                {
                    var hasher = new PasswordHasher<string>();
                    passwordHash = hasher.HashPassword(null, Input.Password);
                }

                int newRoleId = user.RoleId;
                if (User.IsInRole("Admin")) newRoleId = Input.RoleId;

                user.Email = Input.Email;
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.PasswordHash = passwordHash;
                user.RoleId = newRoleId;

                // set modified audit fields
                user.ModifiedAtUtc = DateTime.UtcNow;
                if (current != null)
                {
                    user.ModifiedByUserId = current.Id;
                }

                await _repo.UpdateUserAsync(user);
                _logger.LogInformation("User updated successfully. UserId: {UserId}, Email: {Email}, ModifiedBy: {ModifiedBy}", 
                    Input.Id, Input.Email, User?.Identity?.Name);

                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Account/ShowUsers");
                }
                else
                {
                    return RedirectToPage("/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user. UserId: {UserId}, ModifiedBy: {ModifiedBy}", 
                    Input?.Id, User?.Identity?.Name);
                ModelState.AddModelError("", "Došlo je do greške prilikom spremanja. Molimo pokušajte ponovo.");
                Roles = await _repo.GetAllRolesAsync();
                return Page();
            }
        }
    }
}
