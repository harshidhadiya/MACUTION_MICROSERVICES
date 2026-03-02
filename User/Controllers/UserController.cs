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
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ItokenGeneration token;
        private PasswordHasher<object> hash; private readonly MACUTIONDB _db;
        readonly IMapper mapper;
        public UserController(
            ILogger<UserController> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper)
        {
            this._logger = logger;
            this.hash = hash;
            this.token = token;
            this._db = db;
            this.mapper = mapper;
        }
        [HttpPost("createUser")]
        public async Task<ActionResult> CreateUser(UserCreateDto user)
        {
            try
            {
                var existingUser = await _db.USERS.FirstOrDefaultAsync(x => x.Email == user.Email);
                if (existingUser != null)
                {
                    return BadRequest("User already exists with this email");
                }
                var userData = mapper.Map<UserTable>(user);
                _db.USERS.Add(userData);
                await _db.SaveChangesAsync();
                var generatedToken = token.getToken(
                    userData.Name,
                    userData.Role.ToUpperInvariant(),
                    userData.Id.ToString()
                );
                return Created("/api/v1/user/getprofile", new { name = userData.Name, role = userData.Role, token = generatedToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating user");
                return StatusCode(500, new { message = "An error occurred while creating the user.", detail = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        // This Controller use for the Login of the user and as we
        [HttpPost("login")]
        public async Task<ActionResult> Login(UserLoginDto user)
        {
            var existUser = await _db.USERS.AsNoTracking().FirstOrDefaultAsync(y => y.Email == user.Email);
            if (existUser == null)
            {
                return BadRequest(new { msg = "User Not Exist with this email" });
            }
            var verifyPass = hash.VerifyHashedPassword(new object(), existUser.HashPassword, user.Password);
            if (verifyPass == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { msg = "Incorrecte Password" });
            }
            if (user.Role != existUser.Role)
            {
                return BadRequest(new { msg = "Role Didn't Match" });
            }

            return Ok(new { token = token.getToken(existUser.Name, user.Role.ToUpperInvariant(), existUser.Id.ToString()), Name = existUser.Name, Id = existUser.Id });
        }
        [HttpPost("changepassword")]
        [Authorize]
        public async Task<ActionResult> changePassword(changePasswordDto pass_obj)
        {
            var Id = HttpContext.Items["id"];
            if (!int.TryParse(Id?.ToString(), out var userId))
            {
                return BadRequest("Token Id is not valid.");
            }
            try
            {
                var currentUser = await _db.USERS
                    .FirstOrDefaultAsync(user => user.Id == userId);
                if (currentUser == null)
                    return BadRequest("Current Id relate User Not Exist");
                var hashedPassword = hash.HashPassword(new object(), pass_obj.Password);

                currentUser.HashPassword = hashedPassword;
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing password");
                return StatusCode(500, new { message = "An error occurred while changing the password.", detail = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [HttpPatch("changeprofile")]
        [Authorize]
        public async Task<ActionResult> ChangeProfile(changeProfileDto docs)
        {

            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var userId))
            {
                return BadRequest("Token Id is not valid.");
            }
            var currentUser = await _db.USERS
                .FirstOrDefaultAsync(user => user.Id == userId);

            if (currentUser == null)
            {
                return BadRequest("Current Id relate User Not Exist");
            }


            string tokens = "";


            if (!string.IsNullOrWhiteSpace(docs.Name))
            {
                currentUser.Name = docs.Name;
            }

            if (!string.IsNullOrWhiteSpace(docs.Email))
            {
                currentUser.Email = docs.Email;
            }

            if (docs.Phone != null && !string.IsNullOrWhiteSpace(docs.Phone))
            {
                currentUser.Phone = docs.Phone;
            }

            if (!string.IsNullOrWhiteSpace(docs.Address))
            {
                currentUser.Address = docs.Address;
            }

            if (!string.IsNullOrWhiteSpace(docs.ProfilePicture))
            {
                currentUser.ProfilePicture = docs.ProfilePicture;
            }

            await _db.SaveChangesAsync();

            tokens = token.getToken(
                currentUser.Name,
                currentUser.Role.ToUpperInvariant(),
                currentUser.Id.ToString()
            );

            return Ok(new UserLoginResponseDto { Address = currentUser.Address, Email = currentUser.Email, Name = currentUser.Name, Phone = currentUser.Phone, ProfilePicture = currentUser.ProfilePicture, Role = currentUser.Role, Id = currentUser.Id, Token = tokens });
        }
        [HttpGet("getprofile")]
        [Authorize]
        public async Task<ActionResult> getProfile()
        {
            var id = HttpContext.Items["id"];
            if (!int.TryParse(id?.ToString(), out var userId))
            {
                return BadRequest("Token Id is not valid.");
            }
            var currentUser = await _db.USERS.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId);
            if (currentUser == null)
            {
                return BadRequest("Current Id relate User Not Exist");
            }
            return Ok(new { Address = currentUser.Address, Email = currentUser.Email, Name = currentUser.Name, Phone = currentUser.Phone, ProfilePicture = currentUser.ProfilePicture, Role = currentUser.Role, Id = currentUser.Id });
        }
    }
}