using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.Repositories;
using WienerInsurance.ViewModels;

namespace WienerInsurance.Pages
{
    [Authorize]
    public class ShowPoliciesModel : PageModel
    {
        private readonly PolicyRepository _pRepo;
        private readonly PartnerRepository _partRepo;
        private readonly UserRepository _userRepo;

        public ShowPoliciesModel(PolicyRepository pRepo, PartnerRepository partRepo, UserRepository userRepo)
        {
            _pRepo = pRepo;
            _partRepo = partRepo;
            _userRepo = userRepo;
        }

        public IEnumerable<PolicyDisplayViewModel> Policies { get; set; }
        public IEnumerable<Partner> AllPartners { get; set; }
        public PaginationViewModel Pagination { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public int? SelectedPartnerId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchPolicyNumber { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "active";

        public async Task OnGetAsync()
        {
            AllPartners = await _partRepo.GetAllPartnersAsync(isActive: null);
            bool? isActiveFilter = true;
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                switch (StatusFilter.ToLowerInvariant())
                {
                    case "all": 
                        isActiveFilter = null; 
                        break;
                    case "inactive": 
                        isActiveFilter = false; 
                        break;
                    default: 
                        isActiveFilter = true;
                        break;
                }
            }

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 10;

            var (policies, totalCount) = await _pRepo.GetAllPoliciesPaginatedAsync(
                isActive: isActiveFilter,
                pageNumber: PageNumber,
                pageSize: PageSize
            );

            var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            Pagination = new PaginationViewModel
            {
                CurrentPage = PageNumber,
                PageSize = PageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            };

            var allPolicies = policies.ToList();

            if (SelectedPartnerId.HasValue)
                allPolicies = allPolicies.Where(p => p.PartnerId == SelectedPartnerId).ToList();

            if (!string.IsNullOrEmpty(SearchPolicyNumber))
                allPolicies = allPolicies.Where(p => p.PolicyNumber.Contains(SearchPolicyNumber)).ToList();

            Policies = allPolicies.Select(p => new PolicyDisplayViewModel
            {
                Id = p.Id,
                IsActive = p.IsActive,
                PolicyNumber = p.PolicyNumber,
                Amount = p.Amount,
                PartnerFullName = AllPartners.FirstOrDefault(part => part.Id == p.PartnerId)?.FullName
            }).ToList();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var email = User?.Identity?.Name;
            var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
            var success = await _pRepo.SoftDeletePolicyAsync(id, DateTime.UtcNow, user?.Id ?? 1);
            if (success)
            {
                TempData["Success"] = "Polica je uspješno obrisana.";
            }
            else
            {
                TempData["Error"] = "Greška pri brisanju police.";
            }
            return RedirectToPage(new { StatusFilter = StatusFilter, SelectedPartnerId = SelectedPartnerId, SearchPolicyNumber = SearchPolicyNumber, PageNumber = PageNumber, PageSize = PageSize });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            var email = User?.Identity?.Name;
            var user = email != null ? await _userRepo.GetUserByEmailAsync(email) : null;
            var success = await _pRepo.RestorePolicyAsync(id, DateTime.UtcNow, user?.Id ?? 1);
            if (success)
            {
                TempData["Success"] = "Polica je ponovno aktivirana.";
            }
            else
            {
                TempData["Error"] = "Greška pri aktiviranju police.";
            }
            return RedirectToPage(new { StatusFilter = StatusFilter, SelectedPartnerId = SelectedPartnerId, SearchPolicyNumber = SearchPolicyNumber, PageNumber = PageNumber, PageSize = PageSize });
        }
    }
}