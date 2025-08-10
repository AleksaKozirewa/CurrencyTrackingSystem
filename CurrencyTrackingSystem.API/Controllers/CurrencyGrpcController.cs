using Microsoft.AspNetCore.Mvc;
using CurrencyTrackingSystem.Infrastructure.Services;
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

                //var request = new CurrencyRequest { UserId = userId , Token = $"Bearer {token}" };

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

//[HttpGet("user/{userId}")]
//        [Authorize]
//        public async Task<IActionResult> GetUserCurrencies(int userId)
//        {
//            try
//            {
//                var request = new CurrencyRequest
//                {
//                    UserId = userId.ToString(),
//                    Token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "")
//                };

//                var response = await _grpcClient.GetUserCurrenciesAsync(request);
//                return Ok(response.Currencies);
//            }
//            catch (RpcException ex)
//            {
//                _logger.LogError(ex, "gRPC call failed");
//                //return StatusCode(MapGrpcErrorToHttpStatus(ex.StatusCode), ex.Status.Detail);

//                return StatusCode(500);
//            }
//        }

//        //private static int MapGrpcErrorToHttpStatus(StatusCode grpcStatusCode)
//        //{
//        //    return grpcStatusCode switch
//        //    {
//        //        StatusCode.OK => 200,
//        //        StatusCode.InvalidArgument => 400,
//        //        StatusCode.Unauthenticated => 401,
//        //        StatusCode.PermissionDenied => 403,
//        //        StatusCode.NotFound => 404,
//        //        StatusCode.AlreadyExists => 409,
//        //        StatusCode.FailedPrecondition => 412,
//        //        StatusCode.Internal => 500,
//        //        StatusCode.Unimplemented => 501,
//        //        StatusCode.Unavailable => 503,
//        //        _ => 500
//        //    };
//        //}