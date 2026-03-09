namespace USER.Data.Dto
{
    
    public class SignupResponceDto
    {
        public String Name { get; set; }
        public String Password { get; set; }
        public String Email { get; set; }
        public String Phone { get; set; }
        public String Address { get; set; }
        public String Role { get; set; } = "ADMIN";
        public String? ProfilePicture { get; set; }
        public String token { get; set; }
        public object requestobj{get;set;}
    }
}