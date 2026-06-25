using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;

namespace WienerInsurance.Pages
{
    [Authorize]
    public class CreatePolicyModel : PageModel
    {
        private readonly PartnerRepository _partnerRepo;
        private readonly PolicyRepository _policyRepo;
        private readonly UserRepository _userRepo;
        private readonly ILogger<CreatePolicyModel> _logger;

        public CreatePolicyModel(PartnerRepository partnerRepo, PolicyRepository policyRepo, UserRepository userRepo, ILogger<CreatePolicyModel> logger)
        {
            _partnerRepo = partnerRepo;
            _policyRepo = policyRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        [BindProperty]
        public Policy Policy { get; set; }
        public string ErrorMessage { get; set; }
        public string PartnerFullName { get; set; }
        public string PartnerNumber { get; set; }

        public async Task<IActionResult> OnGetAsync(int partnerId)
        {
            try
            {
                await LoadPartnerInfo(partnerId);
                if (PartnerFullName == null) return NotFound("Partner nije pronađen.");

                Policy = new Policy { PartnerId = partnerId };
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partner info for creating policy. PartnerId: {PartnerId}", partnerId);
                TempData["Error"] = "Došlo je do greške prilikom učitavanja podataka. Molimo pokušajte ponovo.";
                return RedirectToPage("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadPartnerInfo(Policy.PartnerId);
                    return Page();
                }

                var email = User?.Identity?.Name;
                var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
                Policy.CreatedByUserId = user?.Id ?? 1; // fallback to system user
                Policy.CreatedAtUtc = DateTime.UtcNow;

                var success = await _policyRepo.CreatePolicyAsync(Policy);

                if (success)
                {
                    _logger.LogInformation("Policy created successfully. PolicyNumber: {PolicyNumber}, User: {User}", 
                        Policy.PolicyNumber, User?.Identity?.Name);
                    TempData["Success"] = $"Polica ({Policy.PolicyNumber}) je unešena";
                    return new JsonResult(new { success = true, redirectUrl = "/Index" });
                }

                await LoadPartnerInfo(Policy.PartnerId);
                _logger.LogWarning("Failed to create policy. PolicyNumber: {PolicyNumber}", Policy.PolicyNumber);
                return new JsonResult(new { success = false, message = "Greška prilikom unosa police." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating policy. PartnerId: {PartnerId}, User: {User}", 
                    Policy?.PartnerId, User?.Identity?.Name);
                return new JsonResult(new { success = false, message = "Došlo je do greške prilikom unosa police. Molimo pokušajte ponovo." });
            }
        }

        private async Task LoadPartnerInfo(int partnerId)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
            if (partner != null)
            {
                PartnerFullName = $"{partner.FirstName} {partner.LastName}";
                PartnerNumber = partner.PartnerNumber;
            }
        }
    }
}