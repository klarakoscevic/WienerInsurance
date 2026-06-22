using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WienerInsurance.Models;
using WienerInsurance.ViewModels; 

namespace WienerInsurance.Pages.Account
{
    [Authorize(Roles = "Admin")]
    public class ShowUsersModel : PageModel
    {
        private readonly UserRepository _repo;
        public ShowUsersModel(UserRepository repo) => _repo = repo;

        public IEnumerable<UserDisplayViewModel> Users { get; set; }
        public IEnumerable<UserRole> Roles { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RoleId { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _repo.GetAllRolesAsync();

            var allUsers = await _repo.GetAllUsersAsync();

            if (RoleId.HasValue && RoleId.Value != 0)
            {
                allUsers = allUsers.Where(u => u.RoleId == RoleId.Value);
            }

            Users = allUsers.Select(u => new UserDisplayViewModel
            {
                Id = u.Id,
                Email = u.Email,
                FullName = $"{u.FirstName} {u.LastName}",
                RoleName = Roles.FirstOrDefault(g => g.Id == u.RoleId)?.Name,
                CreatedAtUtc = u.CreatedAtUtc,
                ModifiedAtUtc = u.ModifiedAtUtc
            }).ToList();
        }
    }
}
