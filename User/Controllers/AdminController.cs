using ADMIN.Data.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;
using USER.Data.Dto;
using USER.Model;

namespace USER.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly HttpClient httpClient;
        private readonly ItokenGeneration token;
        private PasswordHasher<object> hash; private readonly MACUTIONDB _db;
        readonly IMapper mapper;
        public AdminController(
            ILogger<UserController> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper,IHttpClientFactory httpClientFactory)
        {
            this._logger = logger;
            this.hash = hash;
            this.token = token;
            this._db = db;
            this.mapper = mapper;
            this.httpClient = httpClientFactory.CreateClient("DefaultClient");
        }
        [HttpPost("request/signup")]
        [AllowAnonymous]

        public async Task<ActionResult> requestSignup(UserCreateDto request)
        {
            if (request.Role != "ADMIN")
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Cannot request signup for ADMIN role", 400));
            }

            var existingUser = await _db.USERS.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest("User already exists with this email");
            }
            var userData = mapper.Map<UserTable>(request);
            _db.USERS.Add(userData);
            await _db.SaveChangesAsync();

            // Call the microservice endpoint
           
            var requestBody = new
            {
                RequestUserId = userData.Id
            };
              try
              {
                
            var response = await httpClient.PostAsJsonAsync("/api/request/create", requestBody);
            if (!response.IsSuccessStatusCode)
            {
                var data=await _db.USERS.FindAsync(userData.Id);
                _db.USERS.Remove(data);
                await _db.SaveChangesAsync();
                var errorMessage = await response.Content.ReadAsStringAsync();
                return BadRequest(ApiResponse<object>.ErrorResponse($"Microservice call failed: {errorMessage}", (int)response.StatusCode));
            }

            var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            var result=mapper.Map<SignupResponceDto>(userData);
            result.requestobj=responseData.Data;
            
            return Ok(ApiResponse<object>.SuccessResponse(result, "User created and microservice called successfully"));
              }
              catch (System.Exception)
              {
                
               var data=await _db.USERS.FindAsync(userData.Id);
                _db.USERS.Remove(data);
               await _db.SaveChangesAsync();
              }
          
         return BadRequest(ApiResponse<object>.ErrorResponse("User created and microservice called successfully"));
        }
    }
}