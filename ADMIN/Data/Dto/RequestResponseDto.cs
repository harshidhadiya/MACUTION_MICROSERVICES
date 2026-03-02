using System.ComponentModel.DataAnnotations;

namespace ADMIN.Data.Dto
{
    
    public class VerifyRequestDto
    {
        [Required(ErrorMessage = "RequestId is required")]
        public int RequestId { get; set; }
        
    }

  
    public class GrantUserRightsDto
    {
        public int RequestId { get; set; }
        public int ApprovedByAdminId { get; set; }
    }

 
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

 
    public class CreateRequestDto
    {
        public int RequestUserId { get; set; }
    }
}
