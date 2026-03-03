using System.Collections.Concurrent;
using ADMIN.Data.Dto;
using ADMIN.Middleware.EndPointfilters;
using ADMIN.Model;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;

namespace ADMIN.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    [Authorize(Roles = "ADMIN")]
    public class RequestController : ControllerBase
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _requestLocks = new();
        private readonly ILogger<RequestController> _logger;
        private readonly ItokenGeneration _token;
        private readonly PasswordHasher<object> _hash;
        private readonly MACUTIONDB _db;
        private readonly IMapper _mapper;

        public RequestController(
            ILogger<RequestController> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper)
        {
            _logger = logger;
            _hash = hash;
            _token = token;
            _db = db;
            _mapper = mapper;
        }


        [HttpPost("create")]
        [AllowAnonymous]
        // this call from the user service okay no gateway gonna use here okay 
        public async Task<IActionResult> CreateRequest(CreateRequestDto dto)
        {
            try
            {
                if (dto?.RequestUserId <= 0)
                {
                    _logger.LogWarning("Invalid RequestUserId provided: {UserId}", dto?.RequestUserId);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid user ID. RequestUserId must be greater than 0.",
                        400
                    ));
                }

                var existingRequest = await _db.REQUESTS
                    .FirstOrDefaultAsync(r => r.RequestUserId == dto.RequestUserId && !r.VerifiedByAdmin);

                if (existingRequest != null)
                {
                    _logger.LogWarning("User {UserId} already has a pending request", dto.RequestUserId);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "User already has a pending request. Please wait for admin verification.",
                        400
                    ));
                }

                var request = new RequestTable
                {
                    RequestUserId = dto.RequestUserId,
                    VerifierId = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _db.REQUESTS.Add(request);

                await _db.SaveChangesAsync();

                _logger.LogInformation("Request created successfully for user {UserId}", dto.RequestUserId);

                var responseDto = _mapper.Map<RequestDetailDto>(request);
                return CreatedAtAction(nameof(GetRequestDetails), new { id = request.Id },
                    ApiResponse<RequestDetailDto>.SuccessResponse(
                        responseDto,
                        "Request created successfully",
                        201
                    ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request for user {UserId}", dto?.RequestUserId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while creating the request",
                    500,
                    new List<string> { ex.Message }
                ));
            }
        }


        [HttpGet("verify/{RequestId:int}")]
        [Authorize(Roles = "ADMIN")]
        [TypeFilter(typeof(VerifyFilter))]
        // this directly call from here okay
        public async Task<IActionResult> VerifyRequest(int RequestId)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out int userid))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            try
            {
                if (RequestId <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid RequestId. RequestId must be greater than 0.",
                        400
                    ));
                }
                var semaphore = _requestLocks.GetOrAdd(RequestId, _ => new SemaphoreSlim(1, 1));

                await semaphore.WaitAsync();
                try
                {
                    var request = await _db.REQUESTS.FindAsync(RequestId);
                    if (request == null)
                    {
                        _logger.LogWarning("Request not found: {RequestId}", RequestId);
                        return NotFound(ApiResponse<object>.ErrorResponse(
                            "Request not found",
                            404
                        ));
                    }

                    if (request.VerifiedByAdmin)
                    {
                        _logger.LogWarning("Request {RequestId} is already verified", RequestId);
                        return BadRequest(ApiResponse<object>.ErrorResponse(
                            "This request has already been verified",
                            400
                        ));
                    }


                    request.VerifierId = userid;
                    request.VerifiedByAdmin = true;
                    request.VerifiedAt = DateTime.UtcNow;

                    _db.REQUESTS.Update(request);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "Request {RequestId} verified by admin {userid}",
                        RequestId,
                        userid
                    );

                    var responseDto = _mapper.Map<RequestDetailDto>(request);
                    return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(
                        responseDto,
                        "Request verified successfully"
                    ));
                }
                finally
                {
                    semaphore.Release();
                    _requestLocks.TryRemove(RequestId, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying request {RequestId}", RequestId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while verifying the request",
                    500,
                    new List<string> { ex.Message }
                ));
            }
        }


        [HttpGet("grant-rights/{requestId:int}")]
        [TypeFilter(typeof(VerifyFilter))]
        // this directly call from here okay 
        public async Task<IActionResult> GrantUserRights(int requestId)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out int userid))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            try
            {
                if (requestId <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid RequestId. RequestId must be greater than 0.",
                        400
                    ));
                }

                var request = await _db.REQUESTS.FindAsync(requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestId);
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        "Request not found",
                        404
                    ));
                }

                if (!request.VerifiedByAdmin)
                {
                    _logger.LogWarning(
                        "Attempted to grant rights to request {RequestId} which is not verified",
                        requestId
                    );
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Cannot grant rights: Request must be verified by admin first",
                        400
                    ));
                }

                if (request.RightToAdd)
                {
                    _logger.LogWarning("Request {RequestId} user already has right to add", requestId);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "User already has the right to add other users",
                        400
                    ));
                }
                if (request.VerifierId != userid)
                {
                    _logger.LogWarning("Admin {AdminId} attempted to grant rights to request {RequestId} verified by another admin {VerifierId}",
                        userid, requestId, request.VerifierId);
                    return Forbid();
                }

                request.RightToAdd = true;
                request.RightsGrantedAt = DateTime.UtcNow;

                _db.REQUESTS.Update(request);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Rights granted to request {RequestId} user {UserId} by admin {AdminId}",
                    requestId,
                    request.RequestUserId,
                    userid
                );

                var responseDto = _mapper.Map<RequestDetailDto>(request);
                return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(
                    responseDto,
                    "User rights granted successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting rights to request {RequestId}", requestId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while granting user rights",
                    500,
                    new List<string> { ex.Message }
                ));
            }
        }

        // remove granted rights 
         [HttpGet("revoke-rights/{requestId:int}")]
         [TypeFilter(typeof(VerifyFilter))]
        //  this directly call from here okay
         public async Task<IActionResult> RevokeUserRights(int requestId)
        {          var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out int userid))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            try
            {
                if (requestId <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid RequestId. RequestId must be greater than 0.",
                        400
                    ));
                }

                var request = await _db.REQUESTS.FindAsync(requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestId);
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        "Request not found",
                        404
                    ));
                }

                if (!request.RightToAdd)
                {
                    _logger.LogWarning("Request {RequestId} user does not have right to add", requestId);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "User does not have the right to add other users",
                        400
                    ));
                }
                 if (request.VerifierId != userid)
                {
                    _logger.LogWarning("Admin {AdminId} attempted to revoke rights to request {RequestId} verified by another admin {VerifierId}",
                        userid, requestId, request.VerifierId);
                    return Forbid();
                }

                request.RightToAdd = false;
                request.VerifiedAt = DateTime.UtcNow;

                _db.REQUESTS.Update(request);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Rights revoked for request {RequestId} user {UserId} by admin {AdminId}",
                    requestId,
                    request.RequestUserId,
                    userid
                );

                var responseDto = _mapper.Map<RequestDetailDto>(request);
                return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(
                    responseDto,
                    "User rights revoked successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking rights for request {RequestId}", requestId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while revoking user rights",
                    500,
                    new List<string> { ex.Message }
                ));
            }
        }

        // this below code has been invoked when the user try to verify the product 

        [HttpGet("revoke-verification/{requestId:int}")]
        [TypeFilter(typeof(VerifyFilter))]
        // this directly call from here okay
        public async Task<IActionResult> RevokeVerification(int requestId)
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out int userid))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid user ID in context",
                    400
                ));
            }
            try
            {
                if (requestId <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid RequestId. RequestId must be greater than 0.",
                        400
                    ));
                }

                var request = await _db.REQUESTS.FindAsync(requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", requestId);
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        "Request not found",
                        404
                    ));
                }

                if (!request.VerifiedByAdmin)
                {
                    _logger.LogWarning("Request {RequestId} is already unverified", requestId);
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "This request is already unverified",
                        400
                    ));
                }
                if (request.VerifierId != userid)
                {
                    _logger.LogWarning("Admin {AdminId} attempted to revoke verification for request {RequestId} verified by another admin {VerifierId}",
                        userid, requestId, request.VerifierId);
                    return Forbid();
                }

                request.VerifiedByAdmin = false;
                request.VerifierId = 0;
                request.VerifiedAt = null;

                _db.REQUESTS.Update(request);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Verification revoked for request {RequestId} user {UserId} by admin {AdminId}",
                    requestId,
                    request.RequestUserId,
                    userid
                );

                var responseDto = _mapper.Map<RequestDetailDto>(request);
                return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(
                    responseDto,
                    "Request verification revoked successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking verification for request {RequestId}", requestId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while revoking request verification",
                    500,
                    new List<string> { ex.Message }
                ));
            }
        }

        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid RequestId",
                        400
                    ));
                }

                var request = await _db.REQUESTS.FirstOrDefaultAsync(x => x.RequestUserId == id);

                if (request == null)
                {
                    _logger.LogWarning("Request not found: {RequestId}", id);
                    return NotFound(ApiResponse<object>.ErrorResponse(
                        "Request not found",
                        404
                    ));
                }

                var responseDto = _mapper.Map<RequestDetailDto>(request);
                return Ok(ApiResponse<RequestDetailDto>.SuccessResponse(
                    responseDto,
                    "Request details retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving request details for id {RequestId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while retrieving request details",
                    500
                ));
            }
        }

        // getVerifiedBYunverifiedByme
        // this also used in the userservices to show the request of the user in the admin dashboard
        [HttpGet("user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserRequests(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid UserId",
                        400
                    ));
                }

                var requests = await _db.REQUESTS
                    .Where(r => r.VerifierId == userId)
                    .AsNoTracking()
                    .ToListAsync();

                var responseDtos = _mapper.Map<List<RequestDetailDto>>(requests);

                return Ok(ApiResponse<List<RequestDetailDto>>.SuccessResponse(
                    responseDtos,
                    $"Retrieved {responseDtos.Count} request(s) for user"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving requests for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while retrieving user requests",
                    500
                ));
            }
        }
        //    completed
        //   all pending request has to be shown here okay 
        // this also used in the userservices
        [HttpGet("pending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                Console.WriteLine("entered ;");
                var pendingRequests = await _db.REQUESTS
                    .Where(r => !r.VerifiedByAdmin)
                    .AsNoTracking()
                    .ToListAsync();

                var responseDtos = _mapper.Map<List<RequestDetailDto>>(pendingRequests);
                Console.WriteLine($"Retrieved {responseDtos.Count} pending request(s)");
                return Ok(ApiResponse<List<RequestDetailDto>>.SuccessResponse(
                    responseDtos,
                    $"Retrieved {responseDtos.Count} pending request(s)"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending requests");
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while retrieving pending requests",
                    500
                ));
            }
        }

        // this used in the userservices
        [HttpGet("verified")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVerifiedRequests()
        {
            try
            {
                var verifiedRequests = await _db.REQUESTS
                    .Where(r => r.VerifiedByAdmin)
                    .AsNoTracking()
                    .ToListAsync();

                var responseDtos = _mapper.Map<List<RequestDetailDto>>(verifiedRequests);

                return Ok(ApiResponse<List<RequestDetailDto>>.SuccessResponse(
                    responseDtos,
                    $"Retrieved {responseDtos.Count} verified request(s)"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verified requests");
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while retrieving verified requests",
                    500
                ));
            }
        }
        
    }
}