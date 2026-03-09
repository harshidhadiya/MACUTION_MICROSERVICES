namespace USER.Messaging.Events;

/// <summary>
/// Same shape as ADMIN's event - consumed when ADMIN publishes request.created.
/// </summary>
public sealed class RequestCreatedEvent
{
    public int RequestId { get; set; }
    public int RequestUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
