using ADMIN.Data.Dto;
using ADMIN.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ADMIN.Middleware.EndPointfilters
{
    public class VerifyFilter : IAsyncActionFilter
    {
        private readonly ILogger<VerifyFilter> _logger;
        private readonly MACUTIONDB _db;

        public VerifyFilter(ILogger<VerifyFilter> logger, MACUTIONDB db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            
                var id= context.HttpContext.Items["id"];
                if(!int.TryParse(id?.ToString(),out int userid))
                {
                    context.Result = new BadRequestObjectResult(ApiResponse<object>.ErrorResponse(
                        "Invalid user ID in context",
                        400
                    ));
                    return;
                }   
           var verifier = await _db.REQUESTS.Where(r => r.RequestUserId == userid).FirstOrDefaultAsync();
            if (verifier == null || !verifier.VerifiedByAdmin)
            {
                _logger.LogWarning("User {UserId} attempted to verify a request without proper verification", userid);
                context.Result = new ForbidResult();
                return;
            } 
            await next();
        }
    }
}