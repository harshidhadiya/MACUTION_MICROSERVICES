using ADMIN.Data.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;
using USER.Data.Dto;
using USER.Messaging.Rpc;
using USER.Model;

namespace USER.Data.Interfaces
{
    public class SellerLogin : IsellerLogin
    {
       private readonly ILogger<SellerLogin> _logger;
        private readonly ItokenGeneration token;
        private PasswordHasher<object> hash; private readonly MACUTIONDB _db;
        readonly IMapper mapper;
        public SellerLogin(
            ILogger<SellerLogin> logger,
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
        public async Task<ActionResult> Login(UserLoginDto user,HttpClient ?httpClient)
        {
            var existUser = await _db.USERS.AsNoTracking().FirstOrDefaultAsync(y => y.Email == user.Email);
            if (existUser == null)
            {
                return new BadRequestObjectResult(new { msg = "User Not Exist with this email" });
            }
            // var verifyPass = hash.VerifyHashedPassword(new object(), existUser.HashPassword, user.Password);
            // if (verifyPass == PasswordVerificationResult.Failed)
            // {
            //     return new BadRequestObjectResult(new { msg = "Incorrecte Password" });
            // }
            if (user.Role != existUser.Role)
            {
                return new BadRequestObjectResult(new { msg = "Role Didn't Match" });
            }
            if (user.Role != "SELLER" && user.Role!="USER")
            {
                return new BadRequestObjectResult(new { msg = "Only SELLER or USER role is allowed to login" });
            }
            return new OkObjectResult(new { token = token.getToken(existUser.Name, user.Role.ToUpperInvariant(), existUser.Id.ToString()), Name = existUser.Name, Id = existUser.Id });
        }
    }
    public class AdminLogin : IadminLogin
    {
        private readonly ILogger<AdminLogin>? _logger;
        private readonly ItokenGeneration token;
        private readonly PasswordHasher<object> hash;
        private readonly MACUTIONDB _db;
        private readonly IMapper mapper;

        public AdminLogin(
            ILogger<AdminLogin> logger,
            PasswordHasher<object> hash,
            ItokenGeneration token,
            MACUTIONDB db,
            IMapper mapper)
        {
            _logger = logger;
            this.hash = hash;
            this.token = token;
            _db = db;
            this.mapper = mapper;
        }

        public async Task<ActionResult> Login(UserLoginDto user, IAdminRpcService adminRpc)
        {
            var existUser = await _db.USERS.AsNoTracking().FirstOrDefaultAsync(y => y.Email == user.Email);
            if (existUser == null)
                return new BadRequestObjectResult(new { msg = "User Not Exist with this email" });

            if (user.Role != existUser.Role)
                return new BadRequestObjectResult(new { msg = "Role Didn't Match" });
            if (user.Role != "ADMIN")
                return new BadRequestObjectResult(new { msg = "Only ADMIN role is allowed to login" });

            try
            {
                var requestDetail = await adminRpc.GetRequestDetailsAsync(existUser.Id);
                if (requestDetail == null)
                    return new BadRequestObjectResult(new { message = "Request not found for this admin user" });

                if (!requestDetail.VerifiedByAdmin)
                    return new BadRequestObjectResult(new { message = "Admin request not verified yet" });

                return new OkObjectResult(new
                {
                    token = token.getToken(existUser.Name, user.Role.ToUpperInvariant(), existUser.Id.ToString()),
                    Name = existUser.Name,
                    Id = existUser.Id,
                    RequestObj = requestDetail
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during admin login for user {Email}", user.Email);
                return new BadRequestObjectResult(new { msg = "An error occurred during login", error = ex.Message });
            }
        }
    }
}
