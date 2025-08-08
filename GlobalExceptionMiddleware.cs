using System.Net;
using System.Text.Json;

namespace ADUserManagement.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // AJAX request ise JSON dön
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.ContentType?.Contains("application/json") == true)
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Bir hata oluştu. Lütfen sistem yöneticisi ile iletişime geçin.",
                    Details = _env.IsDevelopment() ? exception.Message : null,
                    StackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };

                var jsonResponse = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(jsonResponse);
            }
            else
            {
                // Normal request ise Error sayfasına yönlendir
                context.Response.Redirect($"/Home/Error?statusCode={context.Response.StatusCode}");
            }
        }
    }

    // Extension method for easy registration
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}