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
        private readonly ILogger<ShowUsersModel> _logger;

        public ShowUsersModel(UserRepository repo, ILogger<ShowUsersModel> logger)
        {
            _repo = repo;
            _logger = logger;
        }

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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users. PageNumber: {PageNumber}, PageSize: {PageSize}, StatusFilter: {StatusFilter}", 
                    PageNumber, PageSize, StatusFilter);
                TempData["Error"] = "Došlo je do greške prilikom učitavanja korisnika. Molimo pokušajte ponovo.";
                Users = new List<UserDisplayViewModel>();
                Roles = new List<UserRole>();
                Pagination = new PaginationViewModel { CurrentPage = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user. UserId: {UserId}, DeletedBy: {DeletedBy}", 
                    id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom brisanja korisnika. Molimo pokušajte ponovo.";
            }
            return RedirectToPage(new { RoleId = RoleId, StatusFilter = StatusFilter, SearchName = SearchName, PageNumber = PageNumber, PageSize = PageSize });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            try
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user. UserId: {UserId}, RestoredBy: {RestoredBy}", 
                    id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom aktiviranja korisnika. Molimo pokušajte ponovo.";
            }
            return RedirectToPage(new { RoleId = RoleId, StatusFilter = StatusFilter, SearchName = SearchName, PageNumber = PageNumber, PageSize = PageSize });
        }
    }
}
