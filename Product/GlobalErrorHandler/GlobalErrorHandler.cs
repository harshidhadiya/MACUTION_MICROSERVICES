using Microsoft.AspNetCore.Diagnostics;
using PRODUCT.Data.Dto;

namespace PRODUCT.GlobalErrorHandler
{
    public class GlobalErrorHandler(ILogger<GlobalErrorHandler> logger, IConfiguration configuration) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode=500;
            if (configuration["development"] == "true")
            {
            logger.LogError(exception, "An unhandled exception occurred.");
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<string>.ErrorResponse(exception.Message, 500), cancellationToken);
                
            }
            else
            {
            logger.LogError(exception, "An unhandled exception occurred.");
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<string>.ErrorResponse("An unexpected error  occurred. Please try again later.", 500), cancellationToken);
            }
            return true;
        }
    }
}