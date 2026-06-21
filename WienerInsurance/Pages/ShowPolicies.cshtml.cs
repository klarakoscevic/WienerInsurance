using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    public class ShowPoliciesModel : PageModel
    {
        private readonly PolicyRepository _pRepo;
        private readonly PartnerRepository _partRepo;

        public ShowPoliciesModel(PolicyRepository pRepo, PartnerRepository partRepo)
        {
            _pRepo = pRepo;
            _partRepo = partRepo;
        }

        public IEnumerable<PolicyDisplayViewModel> Policies { get; set; }
        public IEnumerable<Partner> AllPartners { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedPartnerId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchPolicyNumber { get; set; }

        public async Task OnGetAsync()
        {
            AllPartners = await _partRepo.GetAllPartnersAsync();
            var allPolicies = await _pRepo.GetAllPoliciesAsync();

            if (SelectedPartnerId.HasValue)
                allPolicies = allPolicies.Where(p => p.PartnerId == SelectedPartnerId);

            if (!string.IsNullOrEmpty(SearchPolicyNumber))
                allPolicies = allPolicies.Where(p => p.PolicyNumber.Contains(SearchPolicyNumber));

            Policies = allPolicies.Select(p => new PolicyDisplayViewModel
            {
                PolicyNumber = p.PolicyNumber,
                Amount = p.Amount,
                PartnerFullName = AllPartners.FirstOrDefault(part => part.Id == p.PartnerId)?.FullName
            }).ToList();
        }
    }
}