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
        private readonly ILogger<ShowPoliciesModel> _logger;

        public ShowPoliciesModel(PolicyRepository pRepo, PartnerRepository partRepo, UserRepository userRepo, ILogger<ShowPoliciesModel> logger)
        {
            _pRepo = pRepo;
            _partRepo = partRepo;
            _userRepo = userRepo;
            _logger = logger;
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
            try
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
                    pageSize: PageSize,
                    partnerId: SelectedPartnerId,
                    searchPolicyNumber: SearchPolicyNumber
                );

                var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
                Pagination = new PaginationViewModel
                {
                    CurrentPage = PageNumber,
                    PageSize = PageSize,
                    TotalItems = totalCount,
                    TotalPages = totalPages
                };

                Policies = policies.Select(p => new PolicyDisplayViewModel
                {
                    Id = p.Id,
                    IsActive = p.IsActive,
                    PolicyNumber = p.PolicyNumber,
                    Amount = p.Amount,
                    PartnerFullName = AllPartners.FirstOrDefault(part => part.Id == p.PartnerId)?.FullName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading policies. PageNumber: {PageNumber}, PageSize: {PageSize}, StatusFilter: {StatusFilter}", 
                    PageNumber, PageSize, StatusFilter);
                TempData["Error"] = "Došlo je do greške prilikom učitavanja polica. Molimo pokušajte ponovo.";
                Policies = new List<PolicyDisplayViewModel>();
                AllPartners = new List<Partner>();
                Pagination = new PaginationViewModel { CurrentPage = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            }
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting policy. PolicyId: {PolicyId}, User: {User}", id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom brisanja police. Molimo pokušajte ponovo.";
            }
            return RedirectToPage(new { StatusFilter = StatusFilter, SelectedPartnerId = SelectedPartnerId, SearchPolicyNumber = SearchPolicyNumber, PageNumber = PageNumber, PageSize = PageSize });
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            try
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring policy. PolicyId: {PolicyId}, User: {User}", id, User?.Identity?.Name);
                TempData["Error"] = "Došlo je do greške prilikom aktiviranja police. Molimo pokušajte ponovo.";
            }
            return RedirectToPage(new { StatusFilter = StatusFilter, SelectedPartnerId = SelectedPartnerId, SearchPolicyNumber = SearchPolicyNumber, PageNumber = PageNumber, PageSize = PageSize });
        }
    }
}