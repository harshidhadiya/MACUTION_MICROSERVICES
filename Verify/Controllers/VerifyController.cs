using ADMIN.Data.Dto;
using VERIFY.Data.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VERIFY.Messaging;
using VERIFY.Messaging.Events;
using VERIFY.Messaging.Rpc;
using VERIFY.Model;

// task : create the verify product in the admin pannel and the veriy by admin but there is data has to send like seller id as well as the  flow user -> product -> verify 
// create schema message  for notifying them okay 

// task in product : when you delete the product you have to also unveriy product 

// task : unverify product right so flow from  user ->  unverify 



namespace VERIFY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyController : ControllerBase
    {
        private readonly MACUTIONDB _db;
        private readonly ILogger<VerifyController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRabbitMqPublisher _publisher;
        private readonly IAdminRpcCaller _adminRpcCaller;
        private readonly IUserRpcCaller _userRpcCaller;

        public VerifyController(
            MACUTIONDB db,
            ILogger<VerifyController> logger,
            IHttpClientFactory httpClientFactory,
            IRabbitMqPublisher publisher,
            IAdminRpcCaller adminRpcCaller,
            IUserRpcCaller userRpcCaller)
        {
            _db = db;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _publisher = publisher;
            _adminRpcCaller = adminRpcCaller;
            _userRpcCaller = userRpcCaller;
        }

        public class VerifyProductRequest
        {
            public int ProductId { get; set; }
            public int SellerId { get; set; }
            public string ProductName { get; set; } = string.Empty;
        }

        public class VerifyStatusResponse
        {
            public int ProductId { get; set; }
            public bool IsVerified { get; set; }
            public int? VerifierId { get; set; }
            public DateTime? VerifiedTime { get; set; }
            public string? Description { get; set; }
        }

        // Simple DTOs for reading data from Product and User microservices
        private class ProductSummary
        {
            public int id { get; set; }
            public int? userId { get; set; }
            public string productName { get; set; } = string.Empty;
            public string? description { get; set; }
            public DateTime buyDate { get; set; }
            public DateTime createdDate { get; set; }
        }

        private class UserSummary
        {
            public int id { get; set; }
            public string? name { get; set; }
            public string? email { get; set; }
            public string? role { get; set; }
        }

        private async Task<bool> AdminHasVerifyPermissionAsync(int adminId)
        {
            try
            {
                var requestDetail = await _adminRpcCaller.GetRequestDetailsAsync(adminId);
                if (requestDetail == null)
                {
                    _logger.LogWarning("Admin rights check returned no data for admin {AdminId}", adminId);
                    return false;
                }
                return requestDetail.VerifiedByAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ADMIN RPC for admin {AdminId}", adminId);
                return false;
            }
        }

        private async Task<List<ProductSummary>> GetAllProductsFromProductServiceAsync()
        {
            var client = _httpClientFactory.CreateClient("ProductService");
            var result = new List<ProductSummary>();

            try
            {
                // 1. Create a specific Request object instead of just a URL string
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/product/allproducts");

                // 2. Add the header to the REQUEST, not the client
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    // Use TryAddWithoutValidation to be safe with token formats
                    request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                }

                // 3. Send the specific request object
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch products. Status: {StatusCode}", response.StatusCode);
                    return result;
                }

                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductSummary>>>();
                if (envelope?.Data != null)
                {
                    result = envelope.Data;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Product service");
            }

            return result;
        }

        private async Task<UserSummary?> GetUserFromUserServiceAsync(int? userId)
        {
            if (userId == null || userId <= 0) return null;
            try
            {
                var dto = await _userRpcCaller.GetUserAsync(userId.Value);
                if (dto == null) return null;
                return new UserSummary { id = dto.Id, name = dto.Name, email = dto.Email, role = dto.Role };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling User RPC for user {UserId}", userId);
                return null;
            }
        }

        [HttpPost("product")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> VerifyProduct([FromBody] VerifyProductRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.SellerId <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid request. ProductId and SellerId are required and must be greater than 0.",
                    400
                ));
            }

            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var adminId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid admin id in context.",
                    400
                ));
            }

            // Check that this admin really has authority to verify products
            var hasRights = await AdminHasVerifyPermissionAsync(adminId);
            if (!hasRights)
            {
                return Forbid();
            }

            var existing = await _db.VERIFY_PRODUCTS
                .FirstOrDefaultAsync(v => v.ProductId == request.ProductId);

            if (existing != null)
            {
                existing.SellerId = request.SellerId;
                existing.VerifierId = adminId;
                existing.VerifiedTime = DateTime.UtcNow;
                existing.ProductName = request.ProductName;
                existing.isProductVerified = true;

                if (string.IsNullOrWhiteSpace(existing.Description))
                {
                    existing.Description = "Product verified by admin.";
                }

                _db.VERIFY_PRODUCTS.Update(existing);
            }
            else
            {
                var entity = new VerifyProductTable
                {
                    ProductId = request.ProductId,
                    SellerId = request.SellerId,
                    VerifierId = adminId,
                    VerifiedTime = DateTime.UtcNow,
                    ProductName = request.ProductName,
                    isProductVerified = true,
                    Description = "Product verified by admin."
                };

                await _db.VERIFY_PRODUCTS.AddAsync(entity);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} verified by admin {AdminId}", request.ProductId, adminId);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { request.ProductId, request.SellerId, VerifierId = adminId },
                "Product verified successfully"
            ));
        }

        [HttpDelete("product/{productId:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UnverifyProduct(int productId, [FromBody] string? description = null)
        {
            if (productId <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid product id.",
                    400
                ));
            }

            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var adminId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid admin id in context.",
                    400
                ));
            }

            var hasRights = await AdminHasVerifyPermissionAsync(adminId);
            if (!hasRights)
            {
                return Forbid();
            }

            var record = await _db.VERIFY_PRODUCTS
                .FirstOrDefaultAsync(v => v.ProductId == productId);

            if (record == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Verification record not found for this product",
                    404
                ));
            }

            if (record.VerifierId != adminId)
            {
                _logger.LogWarning("Admin {AdminId} attempted to unverify product {ProductId} verified by another admin {VerifierId}",
                    adminId, productId, record.VerifierId);
                return Forbid();
            }

            record.isProductVerified = false;
            record.VerifiedTime = DateTime.UtcNow;
            record.Description = !string.IsNullOrWhiteSpace(description)
                ? description
                : "Product unverification requested by admin.";

            _db.VERIFY_PRODUCTS.Update(record);
            await _db.SaveChangesAsync();

            _publisher.Publish("product.unverified", new ProductUnverifiedEvent
            {
                ProductId = productId,
            });

            return Ok(ApiResponse<object>.SuccessResponse(
                new { ProductId = productId },
                "Product unverification completed and auction cleared if scheduled"
            ));
        }

        [HttpGet("status/{productId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVerifyStatus(int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid product id.",
                    400
                ));
            }

            var record = await _db.VERIFY_PRODUCTS
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.ProductId == productId);

            if (record == null)
            {
                var notVerified = new VerifyStatusResponse
                {
                    ProductId = productId,
                    IsVerified = false,
                    Description = null
                };

                return Ok(ApiResponse<VerifyStatusResponse>.SuccessResponse(
                    notVerified,
                    "Product is not verified"
                ));
            }

            var response = new VerifyStatusResponse
            {
                ProductId = productId,
                IsVerified = record.isProductVerified,
                VerifierId = record.VerifierId,
                VerifiedTime = record.VerifiedTime,
                Description = record.Description
            };

            return Ok(ApiResponse<VerifyStatusResponse>.SuccessResponse(
                response,
                "Product verification status retrieved successfully"
            ));
        }

        [HttpGet("my-products")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetProductsVerifiedByMe(
            [FromQuery] string? searchName = null)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var adminId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid admin id in context.",
                    400
                ));
            }

            var hasRights = await AdminHasVerifyPermissionAsync(adminId);
            if (!hasRights)
            {
                return Forbid();
            }

            var query = _db.VERIFY_PRODUCTS
                .AsNoTracking()
                .Where(v => v.VerifierId == adminId && v.isProductVerified);

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(v => EF.Functions.Like(v.ProductName, $"%{searchName}%"));
            }

            var verifyRecords = await query
                .OrderByDescending(v => v.VerifiedTime)
                .ToListAsync();

            if (verifyRecords.Count == 0)
            {
                return Ok(ApiResponse<object>.SuccessResponse(
                    new List<object>(),
                    "No verified products found for this admin"
                ));
            }

            var allProducts = await GetAllProductsFromProductServiceAsync();
            if (allProducts.Count == 0)
            {
                return Ok(ApiResponse<object>.SuccessResponse(
                    new List<object>(),
                    "No products found in product service"
                ));
            }

            var productsById = allProducts.ToDictionary(p => p.id, p => p);

            var results = new List<object>();

            foreach (var v in verifyRecords)
            {
                if (productsById.TryGetValue(v.ProductId, out var p))
                {
                    var owner = await GetUserFromUserServiceAsync(p.userId);

                    results.Add(new
                    {
                        id = p.id,
                        productId = p.id,
                        productName = p.productName,
                        description = p.description,
                        buyDate = p.buyDate,
                        createdDate = p.createdDate,
                        ownerId = p.userId,
                        owner = owner == null
                            ? null
                            : new
                            {
                                id = owner.id,
                                name = owner.name,
                                email = owner.email,
                                role = owner.role
                            },
                        verifierId = v.VerifierId,
                        verifiedTime = v.VerifiedTime,
                        isVerified = v.isProductVerified,
                        verifyDescription = v.Description
                    });
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                results,
                "Verified products with details retrieved successfully"
            ));
        }

        [HttpGet("unverified-products")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetUnverifiedProducts(
            [FromQuery] string? searchName = null)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var adminId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid admin id in context.",
                    400
                ));
            }

            var hasRights = await AdminHasVerifyPermissionAsync(adminId);
            if (!hasRights)
            {
                return Forbid();
            }

            var allProducts = await GetAllProductsFromProductServiceAsync();
            if (allProducts.Count == 0)
            {
                return Ok(ApiResponse<object>.SuccessResponse(
                    new List<object>(),
                    "No products found in product service"
                ));
            }

            var verifiedIds = await _db.VERIFY_PRODUCTS
                .AsNoTracking()
                .Where(v => v.isProductVerified)
                .Select(v => v.ProductId)
                .ToListAsync();

            var verifiedSet = new HashSet<int>(verifiedIds);

            IEnumerable<ProductSummary> unverified = allProducts
                .Where(p => !verifiedSet.Contains(p.id));

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                unverified = unverified.Where(p =>
                    !string.IsNullOrEmpty(p.productName) &&
                    p.productName.Contains(searchName, StringComparison.OrdinalIgnoreCase));
            }

            var result = unverified
                .Select(p => new
                {
                    id = p.id,
                    productId = p.id,
                    productName = p.productName,
                    description = p.description,
                    buyDate = p.buyDate,
                    createdDate = p.createdDate,
                    ownerId = p.userId,
                    isVerified = false
                })
                .ToList();

            return Ok(ApiResponse<object>.SuccessResponse(
                result,
                "Unverified products with details retrieved successfully"
            ));
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetVerifyDashboard()
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var adminId))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid admin id in context.", 400));

            var hasRights = await AdminHasVerifyPermissionAsync(adminId);
            if (!hasRights)
                return Forbid();

            var verifiedCount = await _db.VERIFY_PRODUCTS.CountAsync(v => v.isProductVerified);
            var verifiedByMeCount = await _db.VERIFY_PRODUCTS.CountAsync(v => v.VerifierId == adminId && v.isProductVerified);
            var allProducts = await GetAllProductsFromProductServiceAsync();
            var verifiedIds = await _db.VERIFY_PRODUCTS
                .Where(v => v.isProductVerified)
                .Select(v => v.ProductId)
                .ToListAsync();
            var verifiedSet = new HashSet<int>(verifiedIds);
            var unverifiedCount = allProducts.Count(p => !verifiedSet.Contains(p.id));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                adminId,
                verifiedCount,
                verifiedByMeCount,
                unverifiedCount,
                message = "Verify dashboard for admin showcase"
            }, "Verify dashboard"));
        }
    }
}
