using System.ComponentModel.DataAnnotations;

namespace WienerInsurance.ViewModels
{
    public class PartnerInputViewModel
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 255 znakova.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 255 znakova.")]
        public string LastName { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Broj partnera je obavezan.")]
        [RegularExpression(@"^\d{20}$", ErrorMessage = "Broj partnera mora imati točno 20 znamenki.")]
        public string PartnerNumber { get; set; }

        [RegularExpression(@"^\d{11}$", ErrorMessage = "OIB mora imati točno 11 znamenki.")]
        public string? CroatianPIN { get; set; }

        [Required(ErrorMessage = "Tip partnera je obavezan.")]
        [Range(1, 2, ErrorMessage = "Dopuštene vrijednosti su 1 (Personal) ili 2 (Legal).")]
        public int PartnerTypeId { get; set; }

        [Required(ErrorMessage = "Spol je obavezan.")]
        public int GenderId { get; set; }

        public bool IsForeign { get; set; }

        [Required(ErrorMessage = "Vanjska šifra je obavezna.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Vanjska šifra mora imati između 10 i 20 znamenki.")]
        public string ExternalCode { get; set; }
    }
}