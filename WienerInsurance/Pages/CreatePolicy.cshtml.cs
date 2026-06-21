using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;

namespace WienerInsurance.Pages
{
    public class CreatePolicyModel : PageModel
    {
        private readonly PartnerRepository _partnerRepo;
        private readonly PolicyRepository _policyRepo;

        public CreatePolicyModel(PartnerRepository partnerRepo, PolicyRepository policyRepo)
        {
            _partnerRepo = partnerRepo;
            _policyRepo = policyRepo;
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

            var success = await _policyRepo.CreatePolicyAsync(Policy);
            if (success)
            {
                return RedirectToPage("/Index", new { updatedPartnerId = Policy.PartnerId });
            }

            await LoadPartnerInfo(Policy.PartnerId);
            ErrorMessage = "Greška prilikom unosa police.";
            return Page();
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