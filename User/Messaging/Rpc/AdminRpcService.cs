using USER.Data.Dto;

namespace USER.Messaging.Rpc;

public interface IAdminRpcService
{
    Task<RequestDetailDto?> GetRequestDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<RequestDetailDto>?> GetUserRequestsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<RequestDetailDto>?> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminRpcService : IAdminRpcService
{
    private readonly IRpcClient _rpc;

    public AdminRpcService(IRpcClient rpc)
    {
        _rpc = rpc;
    }

    public async Task<RequestDetailDto?> GetRequestDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<RequestDetailsRpcRequest, RpcResponseWrapper<RequestDetailDto>>(
            RpcConstants.RpcExchange,
            RpcConstants.AdminDetailsQueue,
            new RequestDetailsRpcRequest { UserId = userId },
            cancellationToken);
        return response?.Success == true ? response.Data : null;
    }

    public async Task<List<RequestDetailDto>?> GetUserRequestsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<UserListRpcRequest, RpcResponseWrapper<List<RequestDetailDto>>>(
            RpcConstants.RpcExchange,
            RpcConstants.AdminUserListQueue,
            new UserListRpcRequest { UserId = userId },
            cancellationToken);
        return response?.Success == true ? response.Data : null;
    }

    public async Task<List<RequestDetailDto>?> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<object, RpcResponseWrapper<List<RequestDetailDto>>>(
            RpcConstants.RpcExchange,
            RpcConstants.AdminPendingQueue,
            new { },
            cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}
