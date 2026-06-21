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

        public IndexModel(PartnerRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<PartnerViewModel> Partners { get; set; }
        public IEnumerable<PartnerType> PartnerTypes { get; set; }
        public IEnumerable<Gender> Genders { get; set; }

        // Svojstva za pretragu i filtriranje povezujemo kroz GET zahtjev
        [BindProperty(SupportsGet = true)] public string SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchOib { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchPartnerNumber { get; set; }
        [BindProperty(SupportsGet = true)] public int? PartnerType { get; set; }

        [BindProperty(SupportsGet = true)] public int? NewPartnerId { get; set; }
        [BindProperty(SupportsGet = true)] public int? UpdatedPartnerId { get; set; }

        public async Task OnGetAsync()
        {
            PartnerTypes = await _repo.GetPartnerTypesAsync();
            var genders = await _repo.GetGendersAsync();
            var allPartners = await _repo.GetAllPartnersAsync();

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

                return new PartnerViewModel
                {
                    Id = p.Id,
                    FullName = $"{p.FirstName} {p.LastName}",
                    Address = p.Address ?? "-",
                    PartnerNumber = p.PartnerNumber,
                    CroatianPIN = p.CroatianPIN ?? "-",
                    PartnerTypeName = PartnerTypes.FirstOrDefault(t => t.Id == p.PartnerTypeId)?.Name,
                    CreatedAtFormatted = TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local).ToString("dd.MM.yyyy HH:mm"),
                    CreateByUser = p.CreateByUser,
                    IsForeign = p.IsForeign,
                    ExternalCode = p.ExternalCode,
                    GenderName = genders.FirstOrDefault(g => g.Id == p.GenderId)?.Name,
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
                CreateByUser = partner.CreateByUser ?? "-",
                IsForeign = partner.IsForeign,
                ExternalCode = partner.ExternalCode,
                GenderName = genders.FirstOrDefault(g => g.Id == partner.GenderId)?.Name
            });
        }
    }
}