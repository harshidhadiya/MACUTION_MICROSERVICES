namespace ADMIN.Model
{
    public class RequestTable
    {
        public int Id { get; set; }
        public int RequestUserId { get; set; }
        public string Name{get;set;}
        public string Email{get;set;}
        public int VerifierId { get; set; }
        public bool VerifiedByAdmin { get; set; } = false;
        public bool RightToAdd { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? RightsGrantedAt { get; set; }
    }
}