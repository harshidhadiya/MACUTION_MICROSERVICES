namespace PRODUCT.Messaging.Events;

public class RequestVerifyEvent
{
       public int ProductId { get; set; }
       public int SellerId { get; set; }
       public string ProductName { get; set; } = string.Empty;
}