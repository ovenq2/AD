namespace ADUserManagement.Models.Dto
{
    public class ADGroupSearchResultDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string GroupType { get; set; }
        public int MemberCount { get; set; }
        public bool IsUserMember { get; set; }
        public bool IsBuiltIn { get; set; } // ✅ Eksik property eklendi
    }
}