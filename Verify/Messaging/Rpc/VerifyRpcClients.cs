using VERIFY.Data.Dto;

namespace VERIFY.Messaging.Rpc;

public sealed class RequestDetailsRpcRequest
{
    public int UserId { get; set; }
}

public sealed class UserGetRpcRequest
{
    public int UserId { get; set; }
}

public interface IAdminRpcCaller
{
    Task<RequestDetailDto?> GetRequestDetailsAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IUserRpcCaller
{
    Task<UserSummaryDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class UserSummaryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public sealed class AdminRpcCaller : IAdminRpcCaller
{
    private readonly IRpcClient _rpc;

    public AdminRpcCaller(IRpcClient rpc) => _rpc = rpc;

    public async Task<RequestDetailDto?> GetRequestDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<RequestDetailsRpcRequest, RpcResponseWrapper<RequestDetailDto>>(
            RpcConstants.RpcExchange, RpcConstants.AdminDetailsQueue,
            new RequestDetailsRpcRequest { UserId = userId }, cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}

public sealed class UserRpcCaller : IUserRpcCaller
{
    private readonly IRpcClient _rpc;

    public UserRpcCaller(IRpcClient rpc) => _rpc = rpc;

    public async Task<UserSummaryDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<UserGetRpcRequest, RpcResponseWrapper<UserSummaryDto>>(
            RpcConstants.RpcExchange, RpcConstants.UserGetQueue,
            new UserGetRpcRequest { UserId = userId }, cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}
