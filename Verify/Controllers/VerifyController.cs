using ADMIN.Data.Dto;
using VERIFY.Data.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VERIFY.Model;

namespace VERIFY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyController : ControllerBase
    {
        private readonly MACUTIONDB _db;
        private readonly ILogger<VerifyController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public VerifyController(MACUTIONDB db, ILogger<VerifyController> logger, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
        }

        // Simple DTOs for reading data from Product microservice
        private class ProductSummary
        {
            public int id { get; set; }
            public int? userId { get; set; }
            public string productName { get; set; } = string.Empty;
            public string? description { get; set; }
            public DateTime buyDate { get; set; }
            public DateTime createdDate { get; set; }
        }

        private class ProductListEnvelope
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int StatusCode { get; set; }
            public List<ProductSummary>? Data { get; set; }
        }

        private async Task<bool> AdminHasVerifyPermissionAsync(int adminId)
        {
            var client = _httpClientFactory.CreateClient("DefaultClient");

            try
            {
                var response = await client.GetAsync($"/api/request/details/{adminId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Admin rights check failed with status {StatusCode} for admin {AdminId}",
                        response.StatusCode, adminId);
                    return false;
                }

                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<RequestDetailDto>>();
                if (envelope?.Data == null)
                {
                    _logger.LogWarning("Admin rights check returned no data for admin {AdminId}", adminId);
                    return false;
                }

                // Admin is allowed to verify products only if they were verified by another admin
                return envelope.Data.VerifiedByAdmin;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling ADMIN service to check rights for admin {AdminId}", adminId);
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

                var envelope = await response.Content.ReadFromJsonAsync<ProductListEnvelope>();
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
                    ProductName = request.ProductName
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
        public async Task<IActionResult> UnverifyProduct(int productId)
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

            _db.VERIFY_PRODUCTS.Remove(record);
            await _db.SaveChangesAsync();

            // Also clear auction dates on the product side
            var productClient = _httpClientFactory.CreateClient("ProductService");

            // Forward the same Authorization header so Product service can authorize this admin
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                productClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToString());
            }

            try
            {
                var clearResponse = await productClient.PostAsync($"/api/product/{productId}/clear-auction", null);
                if (!clearResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to clear auction for product {ProductId}. Status: {StatusCode}",
                        productId, clearResponse.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Product service to clear auction for product {ProductId}", productId);
            }

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
                    IsVerified = false
                };

                return Ok(ApiResponse<VerifyStatusResponse>.SuccessResponse(
                    notVerified,
                    "Product is not verified"
                ));
            }

            var response = new VerifyStatusResponse
            {
                ProductId = productId,
                IsVerified = true,
                VerifierId = record.VerifierId,
                VerifiedTime = record.VerifiedTime
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
                .Where(v => v.VerifierId == adminId);

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
                    results.Add(new
                    {
                        productId = p.id,
                        productName = p.productName,
                        description = p.description,
                        buyDate = p.buyDate,
                        createdDate = p.createdDate,
                        ownerId = p.userId,
                        verifierId = v.VerifierId,
                        verifiedTime = v.VerifiedTime
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
                    productId = p.id,
                    productName = p.productName,
                    description = p.description,
                    buyDate = p.buyDate,
                    createdDate = p.createdDate,
                    ownerId = p.userId
                })
                .ToList();

            return Ok(ApiResponse<object>.SuccessResponse(
                result,
                "Unverified products with details retrieved successfully"
            ));
        }
    }
}
