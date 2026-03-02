using Microsoft.Identity.Client;

namespace USER.Model
{
    public class UserTable
    {
        public int Id { get; set; }
        public String Name{get; set;}
        public String HashPassword{get;set;}
        public String Email{get;set;}               
        public String Phone{get;set;}
        public String Address{get;set;}
        public String Role{get;set;}="SELLER";
        public String? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}