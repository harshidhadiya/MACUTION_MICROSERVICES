namespace VERIFY.Messaging.Rpc;

public sealed class VerifyStatusRequest
{
    public int ProductId { get; set; }
}

public sealed class VerifyStatusData
{
    public int ProductId { get; set; }
    public bool IsVerified { get; set; }
    public int? VerifierId { get; set; }
    public DateTime? VerifiedTime { get; set; }
    public string? Description { get; set; }
}

public sealed class RpcResponseWrapper<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
