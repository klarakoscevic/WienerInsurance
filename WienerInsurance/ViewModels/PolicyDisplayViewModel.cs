namespace WienerInsurance.ViewModels
{
    public class PolicyDisplayViewModel
    {
        public int Id { get; set; }
        public string PartnerFullName { get; set; }
        public string PolicyNumber { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
    }
}