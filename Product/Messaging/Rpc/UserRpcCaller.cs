namespace PRODUCT.Messaging.Rpc;

public interface IUserRpcCaller
{
    Task<UserGetResponseData?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class UserRpcCaller : IUserRpcCaller
{
    private readonly IRpcClient _rpc;

    public UserRpcCaller(IRpcClient rpc) => _rpc = rpc;

    public async Task<UserGetResponseData?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var response = await _rpc.CallAsync<UserGetRequest, RpcResponseWrapper<UserGetResponseData>>(
            RpcConstants.RpcExchange, RpcConstants.UserGetQueue,
            new UserGetRequest { UserId = userId }, cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}
