namespace VERIFY.Messaging.Events;

public class ProductVerifyEvent
{
    public int productId { get; set; }
    public int verifierId { get; set; }
     public string description { get; set; } = string.Empty;
}