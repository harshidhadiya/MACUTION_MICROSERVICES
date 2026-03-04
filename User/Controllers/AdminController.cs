using ADMIN.Data.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;
using USER.Data.Dto;
using USER.Data.Interfaces;
using USER.Model;

namespace USER.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private  ILogger<UserController> _logger;
        private readonly HttpClient httpClient;
        private readonly ItokenGeneration token;
        private PasswordHasher<object> hash; private readonly MACUTIONDB _db;
        readonly IMapper mapper;
        private readonly IadminLogin adminLogin;
        public AdminController(
            ILogger<UserController> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper,IHttpClientFactory httpClientFactory,IadminLogin
            adminLogin)
        {
            this._logger = logger;
            this.hash = hash;
            this.token = token;
            this._db = db;
            this.mapper = mapper;
            this.httpClient = httpClientFactory.CreateClient("DefaultClient");
            this.adminLogin=adminLogin;
        }
        [HttpPost("request/signup")]

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
        [HttpPost("Login")]
        public async Task<ActionResult> Login(UserLoginDto user)
        {
          return await  adminLogin.Login(user,httpClient);
        }

        [HttpGet("getallverifiedrequests")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> GetAllVerifiedRequests()
        {

           var currentUserId = HttpContext.Items["id"]?.ToString();
            if (!int.TryParse(currentUserId, out var userId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid User ID in token", 400));
            }
            var requests = await httpClient.GetAsync($"/api/Request/user/{userId}");
            if (!requests.IsSuccessStatusCode)
            {
                var error=await requests.Content.ReadFromJsonAsync<ApiResponse<object>>();
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to retrieve verified requests: {error?.Message}", (int)requests.StatusCode));
           }
            
            var responce = await requests.Content.ReadFromJsonAsync<ApiResponse<List<object>>>();
            return Ok(ApiResponse<object>.SuccessResponse(responce?.Data, "All verified requests retrieved successfully " + responce?.Message));
        }

        [HttpGet("pendingrequests")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> GetAllPendingRequests()
        {
            var requests = await httpClient.GetAsync("/api/Request/pending");
            if (!requests.IsSuccessStatusCode)
            {
                var error=await requests.Content.ReadFromJsonAsync<ApiResponse<object>>();
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to retrieve pending requests: {error?.Message}", (int)requests.StatusCode));
            }

            var responce = await requests.Content.ReadFromJsonAsync<ApiResponse<List<RequestDetailDto>>>();

            // Fetch user IDs from the requests
            var requestUserIds = responce.Data?.Select(r => r.RequestUserId).ToList() ?? new List<int>();
            
            var usersFromDb = await _db.USERS
                .Where(u => requestUserIds.Contains(u.Id))
                .ToListAsync();

            var users = usersFromDb.Join(
                responce.Data,
                u => u.Id,
                r => r.RequestUserId,
                (u, r) => new 
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    RequestId = r.Id,
                    VerifiedByAdmin = r.VerifiedByAdmin,
                    HasRightToAdd = r.HasRightToAdd,
                    CreatedAt = r.CreatedAt,
                    VerifiedAt = r.VerifiedAt,
                    RightsGrantedAt = r.RightsGrantedAt
                })
                .ToList();

            return Ok(ApiResponse<object>.SuccessResponse(users, "All pending requests retrieved successfully"));
        }
        }
    }
