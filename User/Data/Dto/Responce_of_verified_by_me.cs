namespace ADMIN.Data.Dto
{
    public class Responce_of_verified_by_me
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