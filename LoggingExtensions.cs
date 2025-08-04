using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ADUserManagement.Constants;

namespace ADUserManagement.Services
{
    /// <summary>
    /// Extension methods for standardized logging with performance monitoring
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs method execution time with automatic performance threshold warnings
        /// </summary>
        public static void LogMethodPerformance(this ILogger logger, string methodName, Stopwatch stopwatch, int? resultCount = null)
        {
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > PerformanceThresholds.VerySlowQueryMs)
            {
                logger.LogError("🐌 VERY SLOW: {MethodName} took {ElapsedMs}ms {ResultInfo}",
                    methodName, elapsedMs, resultCount.HasValue ? $"({resultCount} results)" : "");
            }
            else if (elapsedMs > PerformanceThresholds.SlowQueryMs)
            {
                logger.LogWarning("⚠️ SLOW: {MethodName} took {ElapsedMs}ms {ResultInfo}",
                    methodName, elapsedMs, resultCount.HasValue ? $"({resultCount} results)" : "");
            }
            else
            {
                logger.LogInformation("⚡ {MethodName} completed in {ElapsedMs}ms {ResultInfo}",
                    methodName, elapsedMs, resultCount.HasValue ? $"({resultCount} results)" : "");
            }
        }

        /// <summary>
        /// Logs database query execution with result count
        /// </summary>
        public static void LogDatabaseQuery(this ILogger logger, string queryType, Stopwatch stopwatch, int resultCount)
        {
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            logger.LogInformation(LogMessages.DatabaseQueryOptimized, queryType, resultCount, elapsedMs);

            if (elapsedMs > PerformanceThresholds.SlowQueryMs)
            {
                logger.LogWarning("🐌 Slow database query: {QueryType} - {ResultCount} results in {ElapsedMs}ms",
                    queryType, resultCount, elapsedMs);
            }
        }

        /// <summary>
        /// Logs user action with context information
        /// </summary>
        public static void LogUserAction(this ILogger logger, string action, string username, string? details = null, string? ipAddress = null)
        {
            if (string.IsNullOrEmpty(details))
            {
                logger.LogInformation("👤 User action: {Action} by {Username} from {IPAddress}",
                    action, username, ipAddress ?? "Unknown");
            }
            else
            {
                logger.LogInformation("👤 User action: {Action} by {Username} from {IPAddress} - {Details}",
                    action, username, ipAddress ?? "Unknown", details);
            }
        }

        /// <summary>
        /// Logs admin action with elevated importance
        /// </summary>
        public static void LogAdminAction(this ILogger logger, string action, string adminUser, string targetEntity, string? details = null)
        {
            logger.LogWarning("🛡️ ADMIN ACTION: {Action} by {AdminUser} on {TargetEntity} {Details}",
                action, adminUser, targetEntity, details != null ? $"- {details}" : "");
        }

        /// <summary>
        /// Logs security-related events
        /// </summary>
        public static void LogSecurityEvent(this ILogger logger, string eventType, string username, string details, string? ipAddress = null)
        {
            logger.LogWarning("🔒 SECURITY: {EventType} - User: {Username} from {IPAddress} - {Details}",
                eventType, username, ipAddress ?? "Unknown", details);
        }

        /// <summary>
        /// Logs business logic errors with context
        /// </summary>
        public static void LogBusinessError(this ILogger logger, string operation, string error, string? username = null, object? context = null)
        {
            if (username != null && context != null)
            {
                logger.LogError("❌ Business Error in {Operation}: {Error} - User: {Username} - Context: {@Context}",
                    operation, error, username, context);
            }
            else if (username != null)
            {
                logger.LogError("❌ Business Error in {Operation}: {Error} - User: {Username}",
                    operation, error, username);
            }
            else
            {
                logger.LogError("❌ Business Error in {Operation}: {Error}", operation, error);
            }
        }
    }
}