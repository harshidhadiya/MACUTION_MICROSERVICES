namespace VERIFY.Messaging.Events;

public sealed class ProductUnverifiedEvent
{
    public int ProductId { get; set; }
    public int AdminId { get; set; }
}

