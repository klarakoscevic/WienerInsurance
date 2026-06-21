using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    public class CreatePartnerModel : PageModel
    {
        private readonly PartnerRepository _repo;
        public CreatePartnerModel(PartnerRepository repo) => _repo = repo;
        [BindProperty] public PartnerInputViewModel Input { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
            ViewData["Genders"] = await _repo.GetGendersAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // remove validadtion for props not comming from form
            ModelState.Remove("Partner.CreateByUser");
            ModelState.Remove("Partner.CreatedAtUtc");


            if (!await _repo.IsExternalCodeUniqueAsync(Input.ExternalCode))
            {
                ModelState.AddModelError("Input.ExternalCode", "Navedena Vanjska šifra već postoji.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
                // var errors = ModelState.Values.SelectMany(v => v.Errors);
                return Page();
            }

            var partner = new Partner
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Address = Input.Address,
                PartnerNumber = Input.PartnerNumber,
                CroatianPIN = Input.CroatianPIN,
                PartnerTypeId = Input.PartnerTypeId,
                GenderId = Input.GenderId,
                IsForeign = Input.IsForeign,
                ExternalCode = Input.ExternalCode,
                CreateByUser = "agent.osiguranja@wiener.hr",
                CreatedAtUtc = DateTime.UtcNow
            };

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