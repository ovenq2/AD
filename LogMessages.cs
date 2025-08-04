namespace ADUserManagement.Constants
{
    public static class LogMessages
    {
        // User Validation Messages
        public const string AdUserValidationSuccess = "AD user validation successful for {Username}";
        public const string AdUserValidationFailed = "AD user validation failed for {Username}: {Reason}";
        public const string InvalidUsernameFormat = "Invalid username format: {Username}";
        public const string InvalidPasswordFormat = "Invalid password format provided";
        public const string InvalidGroupNameFormat = "Invalid group name format: {GroupName}";

        // Group Operations Messages
        public const string AdGroupCheckSuccess = "User {Username} is member of group {GroupName}";
        public const string AdGroupCheckFailed = "Error checking group membership for user {Username} in group {GroupName}: {Error}";

        // User Creation Messages
        public const string UserCreationSuccess = "User {Username} created successfully";
        public const string UserCreationFailed = "Failed to create user {Username}: {Error}";

        // User Disable Messages
        public const string UserDisableSuccess = "User {Username} disabled successfully";
        public const string UserDisableFailed = "Failed to disable user {Username}: {Error}";

        // Attribute Update Messages
        public const string AttributeUpdateSuccess = "Attributes updated successfully for user {Username}";
        public const string AttributeUpdateFailed = "Failed to update attributes for user {Username}: {Error}";

        // Group Membership Messages
        public const string GroupMemberAddSuccess = "User {Username} added to group {GroupName} successfully";
        public const string GroupMemberAddFailed = "Failed to add user {Username} to group {GroupName}: {Error}";
        public const string GroupMemberRemoveSuccess = "User {Username} removed from group {GroupName} successfully";
        public const string GroupMemberRemoveFailed = "Failed to remove user {Username} from group {GroupName}: {Error}";

        // Search Messages
        public const string UserSearchSuccess = "User search completed for term: {SearchTerm}";
        public const string UserSearchFailed = "User search failed for term: {SearchTerm}: {Error}";
        public const string GroupSearchSuccess = "Group search completed for term: {SearchTerm}";
        public const string GroupSearchFailed = "Group search failed for term: {SearchTerm}: {Error}";

        // General Error Messages
        public const string UnexpectedError = "An unexpected error occurred: {Error}";
        public const string ContextCreationFailed = "Failed to create AD context: {Error}";
        public const string UserNotFound = "User {Username} not found in Active Directory";
        public const string GroupNotFound = "Group {GroupName} not found in Active Directory";
    }
}