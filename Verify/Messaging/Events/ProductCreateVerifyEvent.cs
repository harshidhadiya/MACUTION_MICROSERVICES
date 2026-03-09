namespace VERIFY.Messaging.Events;

public class ProductCreateVerifyEvent
{
  public int productId { get; set; }
       public int sellerId { get; set; }

       public string productName { get; set; } = string.Empty;

}