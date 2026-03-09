using ADMIN.Data.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;
using USER.Data.Dto;
using USER.Data.Interfaces;
using USER.Messaging;
using USER.Messaging.Rpc;
using USER.Model;

namespace USER.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IAdminRpcService _adminRpc;
        private readonly ItokenGeneration _token;
        private readonly PasswordHasher<object> _hash;
        private readonly MACUTIONDB _db;
        private readonly IMapper _mapper;
        private readonly IadminLogin _adminLogin;
        private readonly IRabbitMqPublisher _publisher;

        public AdminController(
            ILogger<UserController> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper,
            IAdminRpcService adminRpc,
            IadminLogin adminLogin,
            IRabbitMqPublisher publisher)
        {
            _logger = logger;
            _hash = hash;
            _token = token;
            _db = db;
            _mapper = mapper;
            _adminRpc = adminRpc;
            _adminLogin = adminLogin;
            _publisher = publisher;
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
            var userData = _mapper.Map<UserTable>(request);
            _db.USERS.Add(userData);
            await _db.SaveChangesAsync();

           
            var requestBody = new { RequestUserId = userData.Id ,Name=userData.Name,Email=userData.Email};
            try
            {
                _publisher.Publish<object>("request.created",requestBody);           
                return Ok(ApiResponse<object>.SuccessResponse(new{Name=userData.Name,Email=userData.Email,phone=userData.Phone,profilepic=userData.ProfilePicture
                ,Id=userData.Id,tokens=_token.getToken(userData.Name,"ADMIN",userData.Id.ToString())}, "User created and request submitted successfully"));
            }
            catch (Exception)
            {
                var data = await _db.USERS.FindAsync(userData.Id);
                if (data != null)
                {
                    _db.USERS.Remove(data);
                    await _db.SaveChangesAsync();
                }
            }
            return BadRequest(ApiResponse<object>.ErrorResponse("Request creation failed. User created but admin request could not be created.", 400));
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(UserLoginDto user)
        {
            return await _adminLogin.Login(user, _adminRpc);
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
            var responce = await _adminRpc.GetUserRequestsAsync(userId);
            if (responce == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to retrieve verified requests", 503));
            }
            var datas = await _db.USERS.ToListAsync();
            if (responce.Count == 0)
            {
                return Ok(ApiResponse<object>.SuccessResponse(datas, "All verified requests retrieved successfully"));
            }
            var joinedData = datas.Join(responce, x => x.Id, y => y.RequestUserId, (c, p) => new
            {
                Id = c.Id,
                RequestId = p.Id,
                RequestUserId = p.RequestUserId,
                Name = c.Name,
                Address = c.Address,
                Phone = c.Phone,
                profilePicture = c.ProfilePicture,
                isVerified = p.VerifiedByAdmin,
                hasRightToAdd = p.HasRightToAdd,
                verifiedAt = p.VerifiedAt,
                rightsGrantedAt = p.RightsGrantedAt,
                email = c.Email
            });
            return Ok(ApiResponse<object>.SuccessResponse(joinedData, "All verified requests retrieved successfully"));
        }
// WE DON'T REQUIRE THIS ALREADY IN THE  REQUSET CONTROLLER
        [HttpGet("pendingrequests")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> GetAllPendingRequests()
        {
            var responce = await _adminRpc.GetPendingRequestsAsync();
            if (responce == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to retrieve pending requests", 503));
            }

            var requestUserIds = responce.Select(r => r.RequestUserId).ToList();
            var usersFromDb = await _db.USERS
                .Where(u => requestUserIds.Contains(u.Id))
                .ToListAsync();

            var requests1 = responce;
            var users = usersFromDb.Join(
                requests1,
                u => u.Id,
                r => r.RequestUserId,
                (u, r) => new 
                {
                    Id = u.Id,
                    RequestId = r.Id,
                    RequestUserId = r.RequestUserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    verifiedByAdmin = r.VerifiedByAdmin,
                    hasRightToAdd = r.HasRightToAdd,
                    createdAt = r.CreatedAt,
                    verifiedAt = r.VerifiedAt,
                    rightsGrantedAt = r.RightsGrantedAt
                })
                .ToList();

            return Ok(ApiResponse<object>.SuccessResponse(users, "All pending requests retrieved successfully"));
        }
         
        


        [HttpGet("dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult> GetAdminDashboard()
        {
            var currentUserId = HttpContext.Items["id"]?.ToString();
            if (!int.TryParse(currentUserId, out var userId))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid User ID in token", 400));

            var pendingList = await _adminRpc.GetPendingRequestsAsync();
            var verifiedByMe = await _adminRpc.GetUserRequestsAsync(userId);
            var pendingCount = pendingList?.Count ?? 0;

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                pendingRequestCount = pendingCount,
                verifiedByMeCount = verifiedByMe?.Count ?? 0,
                verifiedByMe = verifiedByMe ?? new List<RequestDetailDto>(),
                message = "Admin dashboard data for showcase"
            }, "Admin dashboard"));
        }


    }
}
