using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    public class EditPartnerModel : PageModel
    {
        private readonly PartnerRepository _repo;
        public EditPartnerModel(PartnerRepository repo) => _repo = repo;

        [BindProperty] public PartnerInputViewModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var p = await _repo.GetPartnerByIdAsync(id);
            if (p == null) return NotFound();

            Input = new PartnerInputViewModel
            {
                FirstName = p.FirstName,
                LastName = p.LastName,
                Address = p.Address,
                PartnerNumber = p.PartnerNumber,
                CroatianPIN = p.CroatianPIN,
                PartnerTypeId = p.PartnerTypeId,
                GenderId = p.GenderId,
                IsForeign = p.IsForeign,
                ExternalCode = p.ExternalCode
            };

            ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
            ViewData["Genders"] = await _repo.GetGendersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {

            //ModelState.Remove("Partner.CreateByUser");
            //ModelState.Remove("Partner.CreatedAtUtc");

            var partner = await _repo.GetPartnerByIdAsync(id);

            if (partner.ExternalCode != Input.ExternalCode && !await _repo.IsExternalCodeUniqueAsync(Input.ExternalCode))
            {
                ModelState.AddModelError("Input.ExternalCode", "Navedena Vanjska šifra već postoji.");
            }

            //Partner.Id = id;
            //Partner.CreateByUser = existing.CreateByUser;
            //Partner.CreatedAtUtc = existing.CreatedAtUtc;

            if (!ModelState.IsValid)
            {
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
                var errors = ModelState.Values.SelectMany(v => v.Errors);

                return Page();
            }
            //var partner = await _repo.GetPartnerByIdAsync(id);
            // Mapiranje
            partner.FirstName = Input.FirstName;
            partner.LastName = Input.LastName;
            partner.Address = Input.Address;
            partner.PartnerNumber = Input.PartnerNumber;
            partner.CroatianPIN = Input.CroatianPIN;
            partner.PartnerTypeId = Input.PartnerTypeId;
            partner.GenderId = Input.GenderId;
            partner.IsForeign = Input.IsForeign;
            partner.ExternalCode = Input.ExternalCode;

            var success = await _repo.CreatePartnerAsync(partner);
            if (success)
            {
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError("", "Greška prilikom spremanja.");
            return Page();
        }

    }
}