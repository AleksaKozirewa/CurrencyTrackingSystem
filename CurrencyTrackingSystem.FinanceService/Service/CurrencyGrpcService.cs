using CurrencyTrackingSystem.Application.Interfaces;
using Grpc.Core;

namespace CurrencyTrackingSystem.FinanceService.Service
{
    public class CurrencyGrpcService : CurrencyNewService.CurrencyNewServiceBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly IJwtService _jwtService;

        public CurrencyGrpcService(ICurrencyService currencyService, IJwtService jwtService)
        {
            _currencyService = currencyService;
            _jwtService = jwtService;
        }

        public override async Task<CurrencyResponse> GetUserCurrencies(
            CurrencyRequest request,
            ServerCallContext context)
        {
            // 1. Проверяем авторизацию
            var authHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization");
            if (authHeader == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Token is missing"));

            var token = authHeader.Value.Replace("Bearer ", "");
            var principal = _jwtService.ValidateToken(token);
            if (principal == Guid.Empty)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token"));

            // 2. Проверяем UserId
            if (!Guid.TryParse(request.UserId, out var userId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UserId format"));

            // 3. Получаем данные
            var currencies = await _currencyService.GetUserFavoriteCurrenciesAsync(userId);

            // 4. Формируем ответ
            var response = new CurrencyResponse();
            response.Currencies.AddRange(currencies.Select(c => new CurrencyItem
            {
                Name = c.Name,
                Rate = (double)c.Rate
            }));

            return response;
        }
    }
}