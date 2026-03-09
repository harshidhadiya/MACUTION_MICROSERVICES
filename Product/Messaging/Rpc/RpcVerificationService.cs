using PRODUCT.Services;

namespace PRODUCT.Messaging.Rpc;

public sealed class RpcVerificationService : IVerificationService
{
    private readonly IRpcClient _rpc;
    private readonly ILogger<RpcVerificationService> _logger;

    public RpcVerificationService(IRpcClient rpc, ILogger<RpcVerificationService> logger)
    {
        _rpc = rpc;
        _logger = logger;
    }

    public async Task<bool> IsProductVerifiedAsync(int productId)
    {
        var (isVerified, _) = await GetProductVerificationStatusAsync(productId);
        return isVerified;
    }

    public async Task<(bool IsVerified, string? Description)> GetProductVerificationStatusAsync(int productId)
    {
        var response = await _rpc.CallAsync<VerifyStatusRequest, RpcResponseWrapper<VerifyStatusResponseData>>(
            RpcConstants.RpcExchange, RpcConstants.VerifyStatusQueue,
            new VerifyStatusRequest { ProductId = productId });
        if (response?.Success != true || response.Data == null)
            return (false, null);
        return (response.Data.IsVerified, response.Data.Description);
    }
}
