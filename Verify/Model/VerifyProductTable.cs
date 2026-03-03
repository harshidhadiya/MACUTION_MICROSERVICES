using Microsoft.Identity.Client;

namespace VERIFY.Model
{
    public class VerifyProductTable
    {
       public int Id { get; set; }
       public int userId{get;set;}
       public int verifierId{get;set;}
       public DateTime verified_time{get;set;}
    }
}