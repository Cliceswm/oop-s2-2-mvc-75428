using System.Net;
using Serilog;

namespace FoodSafetyTracker.Web.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception with Serilog
                Log.Error(ex, "Unhandled exception occurred. Path: {Path}, User: {User}",
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "Anonymous");

                // Store the error message for the error page
                context.Items["ErrorMessage"] = ex.Message;

                // Redirect to error page
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}