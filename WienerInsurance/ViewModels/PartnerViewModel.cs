namespace WienerInsurance.ViewModels
{
    public class PartnerViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string PartnerNumber { get; set; }
        public string CroatianPIN { get; set; }
        public string PartnerTypeName { get; set; } 
        public string CreatedAtFormatted { get; set; }
        public string CreateByUser { get; set; }
        public bool IsForeign { get; set; }
        public string ExternalCode { get; set; }
        public string GenderName { get; set; }
    }
}