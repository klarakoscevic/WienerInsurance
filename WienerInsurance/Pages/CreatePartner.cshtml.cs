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
        private readonly ILogger<CreatePartnerModel> _logger;

        public CreatePartnerModel(PartnerRepository repo, UserRepository userRepo, ILogger<CreatePartnerModel> logger)
        {
            _repo = repo;
            _userRepo = userRepo;
            _logger = logger;
        }

        [BindProperty] public PartnerInputViewModel Input { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partner types and genders for create partner page");
                TempData["Error"] = "Došlo je do greške prilikom učitavanja podataka. Molimo pokušajte ponovo.";
                ViewData["PartnerTypes"] = new List<PartnerType>();
                ViewData["Genders"] = new List<Gender>();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
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
                    _logger.LogInformation("Partner created successfully. PartnerNumber: {PartnerNumber}, User: {User}", 
                        Input.PartnerNumber, User?.Identity?.Name);
                    return RedirectToPage("/Index");
                }

                _logger.LogWarning("Failed to create partner. PartnerNumber: {PartnerNumber}", Input.PartnerNumber);
                ModelState.AddModelError("", "Greška prilikom spremanja.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating partner. User: {User}", User?.Identity?.Name);
                ModelState.AddModelError("", "Došlo je do greške prilikom spremanja. Molimo pokušajte ponovo.");
                ViewData["PartnerTypes"] = await _repo.GetPartnerTypesAsync();
                ViewData["Genders"] = await _repo.GetGendersAsync();
                return Page();
            }
        }
    }
}