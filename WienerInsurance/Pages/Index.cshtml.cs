using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    public class IndexModel : PageModel
    {
        private readonly PartnerRepository _repo;
        private readonly UserRepository _userRepo;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(PartnerRepository repo, UserRepository userRepo, ILogger<IndexModel> logger)
        {
            _repo = repo;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var email = User?.Identity?.Name;
                var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;

                var success = await _repo.SoftDeletePartnerAsync(id, DateTime.UtcNow, user?.Id ?? 1);
                if (success)
                {
                    TempData["Success"] = "Partner je uspješno obrisan.";
                }
                else
                {
                    TempData["Error"] = "Greška pri brisanju partnera.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partner. PartnerId: {PartnerId}, DeletedBy: {DeletedBy}", 
                    id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom brisanja partnera. Molimo pokušajte ponovo.";
            }

            return RedirectToPage(new { StatusFilter = StatusFilter, SearchName = SearchName, SearchOib = SearchOib, SearchPartnerNumber = SearchPartnerNumber, PartnerType = PartnerType });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            try
            {
                var email = User?.Identity?.Name;
                var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
                var success = await _repo.RestorePartnerAsync(id, DateTime.UtcNow, user?.Id ?? 1);
                if (success)
                {
                    TempData["Success"] = "Partner je ponovno aktiviran.";
                }
                else
                {
                    TempData["Error"] = "Greška pri aktiviranju partnera.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring partner. PartnerId: {PartnerId}, RestoredBy: {RestoredBy}", 
                    id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom aktiviranja partnera. Molimo pokušajte ponovo.";
            }

            return RedirectToPage(new { StatusFilter = StatusFilter, SearchName = SearchName, SearchOib = SearchOib, SearchPartnerNumber = SearchPartnerNumber, PartnerType = PartnerType });
        }

        public IEnumerable<PartnerViewModel> Partners { get; set; }
        public IEnumerable<PartnerType> PartnerTypes { get; set; }
        public IEnumerable<Gender> Genders { get; set; }
        public PaginationViewModel Pagination { get; set; }

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
        [BindProperty(SupportsGet = true)] public string SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchOib { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchPartnerNumber { get; set; }
        [BindProperty(SupportsGet = true)] public int? PartnerType { get; set; }
        [BindProperty(SupportsGet = true)] public string StatusFilter  { get; set; } = "active"; // active, all, inactive

        [BindProperty(SupportsGet = true)] public int? NewPartnerId { get; set; }
        [BindProperty(SupportsGet = true)] public int? UpdatedPartnerId { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                PartnerTypes = await _repo.GetPartnerTypesAsync() ?? Enumerable.Empty<PartnerType>();
                var genders = await _repo.GetGendersAsync() ?? Enumerable.Empty<Gender>();
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

                var (partners, totalCount) = await _repo.GetAllPartnersPaginatedAsync(
                    pageNumber: PageNumber,
                    pageSize: PageSize,
                    isActive: isActiveFilter,
                    partnerTypeId: PartnerType.HasValue && PartnerType.Value != 0 ? PartnerType.Value : null,
                    searchName: SearchName,
                    searchOib: SearchOib,
                    searchPartnerNumber: SearchPartnerNumber
                );

                var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                Pagination = new PaginationViewModel
                {
                    CurrentPage = PageNumber,
                    PageSize = PageSize,
                    TotalItems = totalCount,
                    TotalPages = totalPages
                };

                Partners = partners.Select(p =>
                {
                    DateTime utcDate = DateTime.SpecifyKind(p.CreatedAtUtc, DateTimeKind.Utc);

                    var displayName = $"{p.FirstName} {p.LastName}";
                    if (p.PolicyCount > 5 || p.TotalPolicyAmount > 5000m)
                    {
                        displayName = "* " + displayName;
                    }

                    return new PartnerViewModel
                    {
                        Id = p.Id,
                        FullName = displayName,
                        Address = p.Address ?? "-",
                        PartnerNumber = p.PartnerNumber,
                        CroatianPIN = p.CroatianPIN ?? "-",
                        PartnerTypeName = PartnerTypes?.FirstOrDefault(t => t.Id == p.PartnerTypeId)?.Name,
                        CreatedAtFormatted = TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm"),
                        CreatedByUserEmail = p.CreatedByUserEmail,
                        IsForeign = p.IsForeign,
                        ExternalCode = p.ExternalCode,
                        GenderName = genders?.FirstOrDefault(g => g.Id == p.GenderId)?.Name,
                        IsActive = p.IsActive,
                        PolicyCount = p.PolicyCount,
                        TotalPolicyAmount = p.TotalPolicyAmount
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partners. PageNumber: {PageNumber}, PageSize: {PageSize}, StatusFilter: {StatusFilter}", 
                    PageNumber, PageSize, StatusFilter);
                TempData["Error"] = "Došlo je do greške prilikom učitavanja podataka. Molimo pokušajte ponovo.";
                Partners = new List<PartnerViewModel>();
                PartnerTypes = new List<PartnerType>();
                Genders = new List<Gender>();
                Pagination = new PaginationViewModel { CurrentPage = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            }
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            try
            {
                var partner = await _repo.GetPartnerByIdAsync(id);
                if (partner == null) return NotFound();

                var types = await _repo.GetPartnerTypesAsync() ?? Enumerable.Empty<PartnerType>();
                var genders = await _repo.GetGendersAsync() ?? Enumerable.Empty<Gender>();

                DateTime utcDate = DateTime.SpecifyKind(partner.CreatedAtUtc, DateTimeKind.Utc);
                string formattedDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm");

                return new JsonResult(new PartnerViewModel
                {
                    Id = partner.Id,
                    FullName = $"{partner.FirstName} {partner.LastName}",
                    Address = partner.Address ?? "-",
                    PartnerNumber = partner.PartnerNumber,
                    CroatianPIN = partner.CroatianPIN ?? "-",
                    PartnerTypeName = types?.FirstOrDefault(t => t.Id == partner.PartnerTypeId)?.Name,
                    CreatedAtFormatted = TimeZoneInfo.ConvertTimeFromUtc(partner.CreatedAtUtc, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm"),
                    CreatedByUserEmail = partner.CreatedByUserEmail ?? "-",
                    IsForeign = partner.IsForeign,
                    ExternalCode = partner.ExternalCode,
                    GenderName = genders?.FirstOrDefault(g => g.Id == partner.GenderId)?.Name,
                    IsActive = partner.IsActive,
                    PolicyCount = partner.PolicyCount,
                    TotalPolicyAmount = partner.TotalPolicyAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partner details. PartnerId: {PartnerId}", id);
                return new JsonResult(new { error = "Došlo je do greške prilikom učitavanja podataka partnera." }) { StatusCode = 500 };
            }
        }
    }
}