using Microsoft.Identity.Client;
using Name;

namespace USER.Data.Dto
{
    public class UserCreateDto
    {
        public String Name { get; set; }
        public String Password { get; set; }
        public String Email { get; set; }
        public String Phone { get; set; }
        public String Address { get; set; }
        public String Role { get; set; } = "SELLER";
        public String? ProfilePicture { get; set; }
    }
    public class UserLoginDto
    {
        public String Email { get; set; }
        public String Password { get; set; }
        public String Role {get;set;}="SELLER";
    }
    public class UserLoginResponseDto
    {
        Tokenget token=new Tokenget();
        public int Id{get;set;}
        public String Name { get; set; }
        public String Email { get; set; }
        public String Role { get; set; }
        public String? ProfilePicture { get; set; }
        public String Phone {get;set;}
        public String Address {get;set;}
        public String Token { get; set; }
        
    }
    public class changePasswordDto
    {
        public String Password { get; set; }
        public String ConfirmPassword { get; set; }
    }

    public class changeProfileDto
    {
        public String ?Name { get; set; }
        public String ?Phone { get; set; }
        public String ?Address { get; set; }
        public String? ProfilePicture { get; set; }
        public String ?Email { get; set; }
    }
}