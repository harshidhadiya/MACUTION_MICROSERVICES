namespace VERIFY.Model
{
    public class VerifyProductTable
    {
       public int Id { get; set; }

       // Product that is being verified for auction
       public int ProductId { get; set; }

       // Owner/seller of the product
       public int SellerId { get; set; }

       // Admin who verified the product
       public int ?VerifierId { get; set; }

       // When the product was verified
       public DateTime ?VerifiedTime { get; set; }

       // Snapshot of product name at the time of verification (for searching)
       public string ProductName { get; set; } = string.Empty;
       public string ?Description {get; set; } = string.Empty;
       public bool isProductVerified { get; set;}
    }
}