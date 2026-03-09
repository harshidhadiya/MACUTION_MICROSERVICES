using Microsoft.Identity.Client;
using Name;

namespace ADMIN.Data.Dto
{
    
    [Obsolete("Use VerifyRequestDto instead")]
    public class acceptrequestDto
    {
        public int verified_admin { get; set; }
        public int requested_user_id { get; set; }
    }
}