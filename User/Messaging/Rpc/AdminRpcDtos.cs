namespace USER.Messaging.Rpc;

public sealed class RequestDetailsRpcRequest
{
    public int UserId { get; set; }
}

public sealed class UserListRpcRequest
{
    public int UserId { get; set; }
}

public sealed class RpcResponseWrapper<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public sealed class RequestDetailRpcDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int RequestUserId { get; set; }
    public int VerifierId { get; set; }
    public bool VerifiedByAdmin { get; set; }
    public bool HasRightToAdd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? RightsGrantedAt { get; set; }
}
