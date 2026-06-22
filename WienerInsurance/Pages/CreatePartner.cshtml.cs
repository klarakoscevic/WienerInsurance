using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    [Authorize]
    public class CreatePartnerModel : PageModel
    {
        private readonly PartnerRepository _repo;
        private readonly UserRepository _userRepo;
        public CreatePartnerModel(PartnerRepository repo, UserRepository userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

        [BindProperty] public PartnerInputViewModel Input { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
            ViewData["Genders"] = await _repo.GetGendersAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await _repo.IsExternalCodeUniqueAsync(Input.ExternalCode))
            {
                ModelState.AddModelError("Input.ExternalCode", "Navedena Vanjska šifra već postoji.");
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
                return Page();
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
            };

            var email = User?.Identity?.Name;
            var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
            partner.CreatedByUserId = user?.Id ?? 1; // fallback to system user
            partner.CreatedAtUtc = DateTime.UtcNow;

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