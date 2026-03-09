namespace PRODUCT.Messaging.Events;

public sealed class ProductDeletedEvent
{
    public int ProductId { get; set; }
    public int DeletedByUserId { get; set; }
}

