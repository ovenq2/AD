namespace ADUserManagement.Models.Dto
{
    public class ADUserSearchResultDto
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public bool IsEnabled { get; set; }
    }
}