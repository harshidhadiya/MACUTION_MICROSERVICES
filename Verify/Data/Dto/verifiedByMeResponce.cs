namespace VERIFY.Data.Dto
{
    public class RequestDetailDto
    {
        public int Id { get; set; }
        public int RequestUserId { get; set; }
        public int VerifierId { get; set; }
        public bool VerifiedByAdmin { get; set; }
        public bool HasRightToAdd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? RightsGrantedAt { get; set; }
    }
    
}