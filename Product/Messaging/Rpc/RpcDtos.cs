namespace PRODUCT.Messaging.Rpc;

public sealed class VerifyStatusRequest
{
    public int ProductId { get; set; }
}

public sealed class VerifyStatusResponseData
{
    public int ProductId { get; set; }
    public bool IsVerified { get; set; }
    public string? Description { get; set; }
}

public sealed class RpcResponseWrapper<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public sealed class UserGetRequest
{
    public int UserId { get; set; }
}

public sealed class UserGetResponseData
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}
