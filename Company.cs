namespace ADUserManagement.Models.Domain
{
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string OUPath { get; set; }
        public bool IsActive { get; set; } = true;
    }
}