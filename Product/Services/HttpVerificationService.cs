using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PRODUCT.Services
{
    public class HttpVerificationService : IVerificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpVerificationService> _logger;

        private class VerifyStatusEnvelope
        {
            public bool Success { get; set; }
            public VerifyStatusData? Data { get; set; }
        }

        private class VerifyStatusData
        {
            public int ProductId { get; set; }
            public bool IsVerified { get; set; }
        }

        public HttpVerificationService(IHttpClientFactory httpClientFactory, ILogger<HttpVerificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> IsProductVerifiedAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient("VerifyService");

            try
            {
                var response = await client.GetAsync($"/api/verify/status/{productId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Verify service returned non-success status {StatusCode} for product {ProductId}",
                        response.StatusCode, productId);
                    return false;
                }

                var envelope = await response.Content.ReadFromJsonAsync<VerifyStatusEnvelope>();
                if (envelope?.Data == null)
                {
                    return false;
                }

                return envelope.Data.IsVerified;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling verify service for product {ProductId}", productId);
                return false;
            }
        }
    }
}

