using ADMIN.Data.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Name;
using VERIFY.Data.Dto;
using VERIFY.Model;

namespace VERIFY.Data.Interfaces
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
            if (user.Role != "SELLER")
            {
                return new BadRequestObjectResult(new { msg = "Only SELLER role is allowed to login" });
            }
            return new OkObjectResult(new { token = token.getToken(existUser.Name, user.Role.ToUpperInvariant(), existUser.Id.ToString()), Name = existUser.Name, Id = existUser.Id });
        }
    }
     public class AdminLogin : IadminLogin
    {
       public readonly ILogger<AdminLogin> ?_logger;
        public  readonly ItokenGeneration token;
        public  PasswordHasher<object> hash; private readonly MACUTIONDB _db;
        public  IMapper mapper;
        public AdminLogin(
            ILogger<AdminLogin> logger,
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
        }
        public async Task<ActionResult> Login(UserLoginDto user, HttpClient httpClient)
        {
            var existUser = await _db.USERS.AsNoTracking().FirstOrDefaultAsync(y => y.Email == user.Email);
            if (existUser == null)
            {
                return new BadRequestObjectResult(new { msg = "User Not Exist with this email" });
            }
            var verifyPass = hash.VerifyHashedPassword(new object(), existUser.HashPassword, user.Password);
            if (verifyPass == PasswordVerificationResult.Failed)
            {
                return new BadRequestObjectResult(new { msg = "Incorrecte Password" });
            }
            if (user.Role != existUser.Role)
            {
                return new BadRequestObjectResult(new { msg = "Role Didn't Match" });
            }
            if (user.Role != "ADMIN")
            {
                return new BadRequestObjectResult(new { msg = "Only ADMIN role is allowed to login" });
            }
            try
            {
                var responce = await httpClient.GetAsync($"/api/request/details/{existUser.Id}");
                
                // Read the response content once
                var content = await responce.Content.ReadFromJsonAsync<ApiResponse<object>>();
                
                // Check if response is successful
                if (!responce.IsSuccessStatusCode)
                {
                 
                        return new BadRequestObjectResult(new { message = content?.Message, errors = content?.Errors });
                 
                }
                return new OkObjectResult(new 
                { 
                    token = token.getToken(existUser.Name, user.Role.ToUpperInvariant(), existUser.Id.ToString()), 
                    Name = existUser.Name, 
                    Id = existUser.Id, 
                    RequestObj = content?.Data 
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