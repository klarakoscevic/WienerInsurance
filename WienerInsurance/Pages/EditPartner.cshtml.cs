using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    [Authorize]
    public class EditPartnerModel : PageModel
    {
        private readonly PartnerRepository _repo;
        private readonly UserRepository _userRepo;
        public EditPartnerModel(PartnerRepository repo, UserRepository userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

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
            var partner = await _repo.GetPartnerByIdAsync(id);

            if (partner.ExternalCode != Input.ExternalCode && !await _repo.IsExternalCodeUniqueAsync(Input.ExternalCode))
            {
                ModelState.AddModelError("Input.ExternalCode", "Navedena Vanjska šifra već postoji.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
                var errors = ModelState.Values.SelectMany(v => v.Errors);

                return Page();
            }

            partner.FirstName = Input.FirstName;
            partner.LastName = Input.LastName;
            partner.Address = Input.Address;
            partner.PartnerNumber = Input.PartnerNumber;
            partner.CroatianPIN = Input.CroatianPIN;
            partner.PartnerTypeId = Input.PartnerTypeId;
            partner.GenderId = Input.GenderId;
            partner.IsForeign = Input.IsForeign;
            partner.ExternalCode = Input.ExternalCode;

            var email = User?.Identity?.Name;
            var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
            partner.ModifiedAtUtc = DateTime.UtcNow;
            partner.ModifiedByUserId = user?.Id;

            await _repo.UpdatePartnerAsync(partner);
            var success = true;
            if (success)
            {
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError("", "Greška prilikom spremanja.");
            return Page();
        }

    }
}