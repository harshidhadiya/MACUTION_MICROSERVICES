using System.Threading.Tasks;

namespace PRODUCT.Services
{
    public interface IVerificationService
    {
        Task<bool> IsProductVerifiedAsync(int productId);
        Task<(bool IsVerified, string? Description)> GetProductVerificationStatusAsync(int productId);
    }
}

