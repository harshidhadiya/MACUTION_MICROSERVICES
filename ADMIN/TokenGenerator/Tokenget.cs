using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Name
{
   public  interface ItokenGeneration
    {
        String getToken(String name,String role,String Id);
    }
    class Tokenget:ItokenGeneration
    {
      
      private readonly String key = "harshidHADIYAHOWAREYOUDFSDFSDSDFGS";

        public String getToken(String name, String role,String Id)
        {
            var claims = new [] {new Claim("Name",name),new Claim(ClaimTypes.Role,role),new Claim("ID",Id)};
            var seckretkey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var SigningCredentials=new SigningCredentials(seckretkey,SecurityAlgorithms.HmacSha256);
            var tokenHandler=new JwtSecurityToken(claims:claims,signingCredentials:SigningCredentials,expires:DateTime.UtcNow.AddDays(1));
            return new JwtSecurityTokenHandler().WriteToken(tokenHandler);
        }
    }
}