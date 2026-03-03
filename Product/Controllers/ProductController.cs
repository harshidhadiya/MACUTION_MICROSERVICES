using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using PRODUCT.Data.Dto;
using PRODUCT.Model;

namespace PRODUCT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController:ControllerBase
    {
        // below section used for the create the product 
        private readonly ILogger<ProductController> _logger;
        private readonly MACUTIONDB _context;
        public ProductController(ILogger<ProductController> logger, MACUTIONDB context)
        {
            _logger = logger;
            _context = context;
        }
        [HttpPost("Create")]
        [Authorize(Roles ="SELLER")]
        public async Task<IActionResult> CreateProduct(createProduct request)
        {
                var userId = HttpContext.Items["id"]?.ToString();
                if(!int.TryParse(userId, out int parsedUserId))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid User Id",403));
                }
                var newProduct = new ProductTable
                {
                    product_name = request.product_name,
                    Buy_Date = request.Buy_Date,
                    product_description = request.product_description,
                    user_id = parsedUserId,
                    creation_date = DateTime.UtcNow
                };
            _context.PRODUCTS.Add(newProduct);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(new {id=newProduct.Id,Productname=newProduct.product_name,Description=newProduct.product_description,buyDate=newProduct.Buy_Date,}, "Product created successfully"));
        
        }

        // keep in mind that you have to here add microservices of the verified here you have to write code of the for in the responce you have to show product is verified or not that all detail okay 
        [HttpGet("GetById/{id:int}")]
        [Authorize(Roles ="SELLER,BUYER")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.PRODUCTS.FindAsync(id);
            if(product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Product not found",404));
            }
            return Ok(ApiResponse<object>.SuccessResponse(new {id=product.Id,Productname=product.product_name,Description=product.product_description,buyDate=product.Buy_Date,}, "Product retrieved successfully"));
        }
  
        //  here also you have to add functionaliy for the deleting okay 
        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if(!int.TryParse(HttpContext.Items["id"].ToString(),out int userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("You Haven't any right to delete this product",401));
            }
            var product = await _context.PRODUCTS.FindAsync(id);
            
            if(product == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Product not found",404));
            
            if (product.user_id != userId)
                return Unauthorized(ApiResponse<object>.ErrorResponse("You Haven't any right to delete this product",401));
            
            _context.PRODUCTS.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse($"succssefully delete product Id = >  {id}"));
        }

        // this endpoint below also may be used by another person
        // also here maybe verified detail you get here also here only for the detail of the particular user
        
        [HttpGet("get/all/products")]
        [Authorize(Roles = "SELLER")]
        public async Task<IActionResult> getAllProduts(
            [FromQuery] string? searchName = null,
            [FromQuery] int? productId = null,
            [FromQuery] DateTime? createdFrom = null,
            [FromQuery] DateTime? createdTo = null,
            [FromQuery] DateTime? buyFrom = null,
            [FromQuery] DateTime? buyTo = null)
        {
            if (!int.TryParse(HttpContext.Items["id"].ToString(), out int userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("You Haven't any right to access this product", 401));

            var query = _context.PRODUCTS.AsNoTracking().Where(x => x.user_id == userId);

            if (productId.HasValue)
                query = query.Where(x => x.Id == productId.Value);

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(x => EF.Functions.Like(x.product_name, $"%{searchName}%"));

            if (createdFrom.HasValue)
                query = query.Where(x => x.creation_date >= createdFrom.Value);
            
            if (createdTo.HasValue)
                query = query.Where(x => x.creation_date <= createdTo.Value);

            if (buyFrom.HasValue)
                query = query.Where(x => x.Buy_Date >= buyFrom.Value);
            
            if (buyTo.HasValue)
                query = query.Where(x => x.Buy_Date <= buyTo.Value);

            var products = await query
                .Select(x => new 
                { 
                    id = x.Id, 
                    productName = x.product_name, 
                    description = x.product_description, 
                    buyDate = x.Buy_Date,
                    createdDate = x.creation_date
                })
                .ToListAsync();

            if (products == null || products.Count == 0)
                return NoContent();

            return Ok(ApiResponse<object>.SuccessResponse(products, "Products retrieved successfully"));
        }
        
        [HttpGet("allproducts")]
        [Authorize]
        public async Task<IActionResult> getAllProductsALLuser(
            [FromQuery] string? searchName = null,
            [FromQuery] int? productId = null,
            [FromQuery] DateTime? createdFrom = null,
            [FromQuery] DateTime? createdTo = null,
            [FromQuery] DateTime? buyFrom = null,
            [FromQuery] DateTime? buyTo = null)
        {
            var query = _context.PRODUCTS.AsNoTracking();

            if (productId.HasValue)
                query = query.Where(x => x.Id == productId.Value);

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(x => EF.Functions.Like(x.product_name, $"%{searchName}%"));

            if (createdFrom.HasValue)
                query = query.Where(x => x.creation_date >= createdFrom.Value);
            
            if (createdTo.HasValue)
                query = query.Where(x => x.creation_date <= createdTo.Value);

            if (buyFrom.HasValue)
                query = query.Where(x => x.Buy_Date >= buyFrom.Value);
            
            if (buyTo.HasValue)
                query = query.Where(x => x.Buy_Date <= buyTo.Value);

            var products = await query
                .Select(x => new 
                { 
                    id = x.Id,
                    userId = x.user_id,
                    productName = x.product_name, 
                    description = x.product_description, 
                    buyDate = x.Buy_Date,
                    createdDate = x.creation_date
                })
                .ToListAsync();

            if (products == null || products.Count == 0)
                return NoContent();

            return Ok(ApiResponse<object>.SuccessResponse(products, "All products retrieved successfully"));
        }
        [HttpPut("update/{id:int}")]
        [Authorize(Roles = "SELLER")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] updateProduct request)
        {
            if (!int.TryParse(HttpContext.Items["id"].ToString(), out int userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("You haven't any right to update this product", 401));

            var product = await _context.PRODUCTS.FindAsync(id);

            if (product == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Product not found", 404));

            if (product.user_id != userId)
                return Unauthorized(ApiResponse<object>.ErrorResponse("You haven't any right to update this product", 401));

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.product_name))
                product.product_name = request.product_name;

            if (!string.IsNullOrEmpty(request.product_description))
                product.product_description = request.product_description;

            if (request.Buy_Date.HasValue)
                product.Buy_Date = request.Buy_Date.Value;

            _context.PRODUCTS.Update(product);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new 
            { 
                id = product.Id, 
                productName = product.product_name, 
                description = product.product_description, 
                buyDate = product.Buy_Date,
                createdDate = product.creation_date
            }, "Product updated successfully"));
        }
    }
}



