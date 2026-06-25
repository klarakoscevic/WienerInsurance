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
        public IndexModel(PartnerRepository repo, UserRepository userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
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

            return RedirectToPage(new { StatusFilter = StatusFilter, SearchName = SearchName, SearchOib = SearchOib, SearchPartnerNumber = SearchPartnerNumber, PartnerType = PartnerType });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
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

            return RedirectToPage(new { StatusFilter = StatusFilter, SearchName = SearchName, SearchOib = SearchOib, SearchPartnerNumber = SearchPartnerNumber, PartnerType = PartnerType });
        }

        public IEnumerable<PartnerViewModel> Partners { get; set; }
        public IEnumerable<PartnerType> PartnerTypes { get; set; }
        public IEnumerable<Gender> Genders { get; set; }

        [BindProperty(SupportsGet = true)] public string SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchOib { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchPartnerNumber { get; set; }
        [BindProperty(SupportsGet = true)] public int? PartnerType { get; set; }
        [BindProperty(SupportsGet = true)] public string StatusFilter  { get; set; } = "active"; // active, all, inactive

        [BindProperty(SupportsGet = true)] public int? NewPartnerId { get; set; }
        [BindProperty(SupportsGet = true)] public int? UpdatedPartnerId { get; set; }

        public async Task OnGetAsync()
        {
            PartnerTypes = await _repo.GetPartnerTypesAsync();
            var genders = await _repo.GetGendersAsync();
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

            var allPartners = await _repo.GetAllPartnersAsync();

            if (isActiveFilter.HasValue)
            {
                allPartners = allPartners.Where(p => (p.IsActive) == isActiveFilter.Value);
            }

            if (!string.IsNullOrEmpty(SearchName))
                allPartners = allPartners.Where(p => p.FirstName.Contains(SearchName, StringComparison.OrdinalIgnoreCase) || p.LastName.Contains(SearchName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(SearchOib))
                allPartners = allPartners.Where(p => p.CroatianPIN?.Contains(SearchOib) == true);

            if (!string.IsNullOrEmpty(SearchPartnerNumber))
                allPartners = allPartners.Where(p => p.PartnerNumber?.Contains(SearchPartnerNumber) == true);

            if (PartnerType.HasValue && PartnerType.Value != 0)
                allPartners = allPartners.Where(p => p.PartnerTypeId == PartnerType.Value);

            Partners = allPartners.Select(p =>
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
                    PartnerTypeName = PartnerTypes.FirstOrDefault(t => t.Id == p.PartnerTypeId)?.Name,
                    CreatedAtFormatted = TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm"),
                    CreatedByUserEmail = p.CreatedByUserEmail,
                    IsForeign = p.IsForeign,
                    ExternalCode = p.ExternalCode,
                    GenderName = genders.FirstOrDefault(g => g.Id == p.GenderId)?.Name,
                    IsActive = p.IsActive,
                    PolicyCount = p.PolicyCount,
                    TotalPolicyAmount = p.TotalPolicyAmount
                };
            }).ToList();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var partner = await _repo.GetPartnerByIdAsync(id);
            if (partner == null) return NotFound();

            var types = await _repo.GetPartnerTypesAsync();
            var genders = await _repo.GetGendersAsync();

            DateTime utcDate = DateTime.SpecifyKind(partner.CreatedAtUtc, DateTimeKind.Utc);
            string formattedDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm");

            return new JsonResult(new PartnerViewModel
            {
                Id = partner.Id,
                FullName = $"{partner.FirstName} {partner.LastName}",
                Address = partner.Address ?? "-",
                PartnerNumber = partner.PartnerNumber,
                CroatianPIN = partner.CroatianPIN ?? "-",
                PartnerTypeName = types.FirstOrDefault(t => t.Id == partner.PartnerTypeId)?.Name,
                CreatedAtFormatted = TimeZoneInfo.ConvertTimeFromUtc(partner.CreatedAtUtc, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm"),
                CreatedByUserEmail = partner.CreatedByUserEmail ?? "-",
                IsForeign = partner.IsForeign,
                ExternalCode = partner.ExternalCode,
                GenderName = genders.FirstOrDefault(g => g.Id == partner.GenderId)?.Name,
                IsActive = partner.IsActive,
                PolicyCount = partner.PolicyCount,
                TotalPolicyAmount = partner.TotalPolicyAmount
            });
        }
    }
}