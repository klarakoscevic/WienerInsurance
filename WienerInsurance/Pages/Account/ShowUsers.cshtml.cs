using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class ShowUsersModel : PageModel
    {
        private readonly UserRepository _repo;
        public ShowUsersModel(UserRepository repo) => _repo = repo;

        public IEnumerable<UserDisplayViewModel> Users { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }
        public PaginationViewModel Pagination { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public int? RoleId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "active";

        public async Task OnGetAsync()
        {
            Roles = await _repo.GetAllRolesAsync();

            bool? isActiveFilter = true;
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                switch (StatusFilter.ToLowerInvariant())
                {
                    case "all":
                        isActiveFilter = null;
                        break;
                    case "inactive":
                        isActiveFilter = false;
                        break;
                    default:
                        isActiveFilter = true;
                        break;
                }
            }

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;

            var (users, totalCount) = await _repo.GetAllUsersPaginatedAsync(
                pageNumber: PageNumber,
                pageSize: PageSize,
                isActive: isActiveFilter,
                roleId: RoleId.HasValue && RoleId.Value != 0 ? RoleId.Value : null,
                searchName: SearchName
            );

            var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            Pagination = new PaginationViewModel
            {
                CurrentPage = PageNumber,
                PageSize = PageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            };

            Users = users.Select(u => new UserDisplayViewModel
            {
                Id = u.Id,
                Email = u.Email,
                FullName = $"{u.FirstName} {u.LastName}",
                RoleName = Roles.FirstOrDefault(g => g.Id == u.RoleId)?.Name,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                ModifiedAtUtc = u.ModifiedAtUtc
            }).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var email = User?.Identity?.Name;
            var user = email != null ? await _repo.GetUserByEmailAsync(email) : null;
            var success = await _repo.SoftDeleteUserAsync(id, DateTime.UtcNow, user?.Id ?? 1);
            if (success)
            {
                TempData["Success"] = "Korisnik je uspješno obrisan.";
            }
            else
            {
                TempData["Error"] = "Greška pri brisanju korisnika.";
            }
            return RedirectToPage(new { RoleId = RoleId, StatusFilter = StatusFilter, SearchName = SearchName, PageNumber = PageNumber, PageSize = PageSize });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            var email = User?.Identity?.Name;
            var user = email != null ? await _repo.GetUserByEmailAsync(email) : null;
            var success = await _repo.RestoreUserAsync(id, DateTime.UtcNow, user?.Id ?? 1);
            if (success)
            {
                TempData["Success"] = "Korisnik je ponovno aktiviran.";
            }
            else
            {
                TempData["Error"] = "Greška pri aktiviranju korisnika.";
            }
            return RedirectToPage(new { RoleId = RoleId, StatusFilter = StatusFilter, SearchName = SearchName, PageNumber = PageNumber, PageSize = PageSize });
        }
    }
}
