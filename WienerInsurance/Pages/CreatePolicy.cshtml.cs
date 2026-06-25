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

        public CreatePolicyModel(PartnerRepository partnerRepo, PolicyRepository policyRepo, UserRepository userRepo)
        {
            _partnerRepo = partnerRepo;
            _policyRepo = policyRepo;
            _userRepo = userRepo;
        }

        [BindProperty]
        public Policy Policy { get; set; }
        public string ErrorMessage { get; set; }
        public string PartnerFullName { get; set; }
        public string PartnerNumber { get; set; }

        public async Task<IActionResult> OnGetAsync(int partnerId)
        {
            await LoadPartnerInfo(partnerId);
            if (PartnerFullName == null) return NotFound("Partner nije pronađen.");

            Policy = new Policy { PartnerId = partnerId };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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
                TempData["Success"] = $"Polica ({Policy.PolicyNumber}) je unešena";
                return new JsonResult(new { success = true, redirectUrl = "/Index" });
            }

            await LoadPartnerInfo(Policy.PartnerId);
            return new JsonResult(new { success = false, message = "Greška prilikom unosa police." });
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