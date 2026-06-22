using System.ComponentModel.DataAnnotations;

namespace WienerInsurance.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }
        public string OriginalEmail { get; set; }

        [Required(ErrorMessage = "Ime je obavezno.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Neispravan format emaila.")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Rola je obavezna.")]
        public int RoleId { get; set; }
    }
}