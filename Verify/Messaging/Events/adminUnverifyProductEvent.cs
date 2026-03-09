namespace VERIFY.Messaging.Events;

public class adminUnverifyProductEvent
{
  public int productId{get;set;}
  public int adminId{get;set;}
    public string description { get; set; } = string.Empty;
}