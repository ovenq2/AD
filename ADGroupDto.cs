namespace ADUserManagement.Models.Dto
{
    public class ADGroupDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string GroupType { get; set; } // Security, Distribution
        public string Scope { get; set; } // Domain Local, Global, Universal
        public int MemberCount { get; set; }
        public bool IsBuiltIn { get; set; }
    }
}