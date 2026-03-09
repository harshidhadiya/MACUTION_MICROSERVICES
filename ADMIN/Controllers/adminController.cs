using ADMIN.Data.Dto;
using ADMIN.Messaging;
using ADMIN.Middleware.EndPointfilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ADMIN.Controllers
{
    [ApiController]
    [Route("api/Request/[controller]")]
    public class adminController : ControllerBase
    {
        private readonly IRabbitMqPublisher _publisher;
        public class sendDataVerifyProduct
        {
            public int ProductId { get; set; }
            public int verifierId { get; set; }
            public string Description { get; set; } = string.Empty;
        }
        public adminController(IRabbitMqPublisher publisher)
        {
            this._publisher = publisher;
        }
        [HttpPost("verify")]
        [Authorize(Roles = "ADMIN")]
        [TypeFilter(typeof(VerifyFilter))]
        public async Task<IActionResult> verifyProduct(ProductVerify request)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out int userid))
            {
                return new BadRequestObjectResult(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data"));
            }

            var data = new sendDataVerifyProduct
            {
                ProductId = request.ProductId,
                verifierId = userid,
                Description = request.Description
            };

            _publisher.Publish("product.verify", data);

            return Ok(ApiResponse<object>.SuccessResponse(new { ProductId = request.ProductId, VerifierId = userid }, "Product verification request sent successfully"));
        }
        [HttpDelete("unverify/{id:int}")]

        [Authorize(Roles = "ADMIN")]
        [TypeFilter(typeof(VerifyFilter))]
        public async Task<IActionResult> unverifyProduct(int id,string description="")
        {
            var idq = HttpContext.Items["id"];
            if (!int.TryParse(idq?.ToString(), out int userid))
            {
                return new BadRequestObjectResult(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data"));
            }
            _publisher.Publish<object>("admin.unverify",new { productId = id, adminId = userid, description = description!=""?description :"Unverified by admin" });
            return Ok(ApiResponse<object>.SuccessResponse(new { ProductId = id }, "Product unverification request sent successfully"));
    }
    }
}