using Microsoft.AspNetCore.Mvc;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using CurrencyTrackingSystem.FinanceService;

namespace CurrencyTrackingSystem.API.Controllers
{
    [ApiController]
    [Route("api/grpc/currencies")]
    public class CurrencyGrpcController : ControllerBase
    {
        private readonly CurrencyNewService.CurrencyNewServiceClient _grpcClient;
        private readonly ILogger<CurrencyGrpcController> _logger;

        public CurrencyGrpcController(
            CurrencyNewService.CurrencyNewServiceClient grpcClient,
            ILogger<CurrencyGrpcController> logger)
        {
            _grpcClient = grpcClient;
            _logger = logger;
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserCurrencies(string userId)
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .ToString()
                    .Replace("Bearer ", "");

                var request = new CurrencyRequest { UserId = userId };

                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var response = await _grpcClient.GetUserCurrenciesAsync(request, headers: headers);

                return Ok(response.Currencies);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
            {
                return Unauthorized(ex.Status.Detail);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error for user {UserId}", userId);
                return StatusCode(500, "Service unavailable");
            }
        }
    }
}