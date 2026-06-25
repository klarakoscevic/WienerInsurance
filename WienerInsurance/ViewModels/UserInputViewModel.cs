using System.ComponentModel.DataAnnotations;

namespace WienerInsurance.ViewModels
{
    public class UserInputViewModel
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Neispravan format emaila.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Lozinka mora imati najmanje 8 znakova.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Lozinka mora sadržavati veliko slovo, malo slovo, broj ispecijalni znak.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Rola je obavezna.")]
        [Range(1, int.MaxValue, ErrorMessage = "Molimo odaberite valjanu rolu.")]
        public int RoleId { get; set; }
    }
}