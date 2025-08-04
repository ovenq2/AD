namespace ADUserManagement.Constants
{
    /// <summary>
    /// Structured logging message templates
    /// </summary>
    public static class LogMessages
    {
        // Request Service Messages
        public const string UserCreationRequestCreated = "User creation request created: {RequestId} for user {Username} by {RequestedBy}";
        public const string UserCreationRequestFailed = "Failed to create user creation request for {Username} by {RequestedBy}: {Error}";

        public const string UserDeletionRequestCreated = "User deletion request created: {RequestId} for user {Username} by {RequestedBy}";
        public const string UserDeletionRequestFailed = "Failed to create user deletion request for {Username} by {RequestedBy}: {Error}";

        public const string AttributeChangeRequestCreated = "Attribute change request created: {RequestId} for user {Username} attribute {AttributeName} by {RequestedBy}";
        public const string AttributeChangeRequestFailed = "Failed to create attribute change request for {Username} by {RequestedBy}: {Error}";

        public const string PendingRequestsRetrieved = "Retrieved {RequestCount} pending requests using optimized query in {ElapsedMs}ms";
        public const string PendingRequestsFallback = "Using fallback method for pending requests due to SQL error";
        public const string PendingRequestsError = "Error retrieving pending requests: {Error}";

        public const string MyRequestsRetrieved = "Retrieved {RequestCount} requests for user {Username} using optimized query in {ElapsedMs}ms";
        public const string MyRequestsFallback = "Using fallback method for user {Username} requests due to SQL error";
        public const string MyRequestsError = "Error retrieving requests for user {Username}: {Error}";

        // Authentication Messages
        public const string LoginAttemptSuccess = "Successful login for user {Username} from {IPAddress}";
        public const string LoginAttemptFailed = "Failed login attempt for user {Username} from {IPAddress}: {Reason}";
        public const string LoginAccessDenied = "Access denied for user {Username}: not in required groups (HelpDesk or SysNet)";
        public const string UserLoggedOut = "User {Username} logged out successfully";

        // Password Service Messages
        public const string PasswordResetRequestCreated = "Password reset request created: {RequestId} for user {Username} by {RequestedBy}";
        public const string PasswordResetRequestApproved = "Password reset approved: {RequestId} for user {Username} by {ApprovedBy}";
        public const string PasswordResetRequestRejected = "Password reset rejected: {RequestId} for user {Username} by {RejectedBy} with reason: {Reason}";
        public const string PasswordResetFailed = "Failed to reset password for user {Username}: {Error}";

        // Active Directory Messages
        public const string AdUserValidationSuccess = "AD user validation successful for {Username}";
        public const string AdUserValidationFailed = "AD user validation failed for {Username}: {Error}";
        public const string AdGroupCheckSuccess = "AD group check successful: user {Username} is member of {GroupName}";
        public const string AdGroupCheckFailed = "AD group check failed for user {Username} and group {GroupName}: {Error}";
        public const string AdUserCreated = "AD user created successfully: {Username}";
        public const string AdUserCreationFailed = "Failed to create AD user {Username}: {Error}";

        // Email Service Messages
        public const string EmailSentSuccess = "Email sent successfully to {Recipient} with subject: {Subject}";
        public const string EmailSendFailed = "Failed to send email to {Recipient}: {Error}";
        public const string EmailNotificationSent = "Request notification email sent for {RequestType} request {RequestNumber}";
        public const string EmailNotificationFailed = "Failed to send notification email for request {RequestId}: {Error}";

        // Performance Messages
        public const string DatabaseQueryExecuted = "Database query executed: {QueryType} completed in {ElapsedMs}ms";
        public const string DatabaseQueryOptimized = "Optimized query used for {Operation} - {ResultCount} results in {ElapsedMs}ms";
        public const string DatabaseQueryFallback = "Fallback query used for {Operation} due to optimization failure";

        // Admin Actions
        public const string RequestApproved = "Request approved: {RequestType} {RequestId} by {ApprovedBy}";
        public const string RequestRejected = "Request rejected: {RequestType} {RequestId} by {RejectedBy} with reason: {Reason}";
        public const string AdminActionFailed = "Admin action failed: {Action} on {RequestType} {RequestId} by {User}: {Error}";

        // Application Lifecycle
        public const string ApplicationStarting = "🚀 ADUserManagement application starting up - Version: {Version}";
        public const string ApplicationStarted = "✅ ADUserManagement application started successfully on {Environment}";
        public const string ApplicationStopping = "🛑 ADUserManagement application shutting down";
        public const string ApplicationError = "💥 Critical application error: {Error}";

        // Validation Messages
        public const string InvalidUsernameFormat = "Invalid username format provided: {Username}";
        public const string InvalidEmailFormat = "Invalid email format provided: {Email}";
        public const string InvalidGroupNameFormat = "Invalid group name format provided: {GroupName}";
        public const string ValidationPassed = "Input validation passed for {InputType}: {Value}";
        public const string ValidationFailed = "Input validation failed for {InputType}: {Value} - {ValidationError}";
    }

    /// <summary>
    /// Performance measurement thresholds
    /// </summary>
    public static class PerformanceThresholds
    {
        public const int SlowQueryMs = 1000;      // Queries slower than 1s
        public const int VerySlowQueryMs = 5000;  // Queries slower than 5s
        public const int SlowRequestMs = 2000;    // HTTP requests slower than 2s
        public const int VerySlowRequestMs = 10000; // HTTP requests slower than 10s
    }
}