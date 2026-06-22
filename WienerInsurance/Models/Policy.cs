using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WienerInsurance.Models
{
    public class Policy
    {
        public int Id { get; set; }
        public int PartnerId { get; set; }

        [Required(ErrorMessage = "Broj police je obavezan.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Broj police mora imati između 10 i 15 znakova.")]
        public string PolicyNumber { get; set; }

        [Required(ErrorMessage = "Iznos police je obavezan.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Iznos police mora biti veći od 0.")]
        public decimal Amount { get; set; }    
        public DateTime CreatedAtUtc { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public int? ModifiedByUserId { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string? PartnerFullName { get; set; }
    }
}