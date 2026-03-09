namespace ADMIN.Messaging.Events;

public sealed class RequestCreatedEvent
{
    public int RequestId { get; set; }
    public int RequestUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class CreateRequest
{
    public int requestUserId { get; set; }
    public string name{get;set;}
    public string email {get;set;}
}

