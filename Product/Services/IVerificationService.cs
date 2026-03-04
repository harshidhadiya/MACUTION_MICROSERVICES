using System.Threading.Tasks;

namespace PRODUCT.Services
{
    public interface IVerificationService
    {
        Task<bool> IsProductVerifiedAsync(int productId);
    }
}

