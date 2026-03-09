namespace VERIFY.Messaging.Events;

public sealed class ProductDeletedEvent
{
    public int productId { get; set; }
    public int deletedByUserId { get; set; }
}

