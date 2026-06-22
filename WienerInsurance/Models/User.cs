namespace WienerInsurance.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public int RoleId { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public int? ModifiedByUserId { get; set; }
    }
}