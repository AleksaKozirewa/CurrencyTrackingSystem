using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Configuration;

namespace CurrencyTrackingSystem.API.Filters
{
    public class YarpSwaggerFilter : IDocumentFilter
    {
        private readonly IProxyConfigProvider _proxyConfig;

        public YarpSwaggerFilter(IProxyConfigProvider proxyConfig)
        {
            _proxyConfig = proxyConfig;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var proxyConfig = _proxyConfig.GetConfig();

            foreach (var route in proxyConfig.Routes)
            {
                // Новый способ получения пути
                var path = route.Match.Path;

                if (string.IsNullOrEmpty(path))
                    continue;

                // Убедимся, что путь начинается с /
                if (!path.StartsWith("/"))
                    path = "/" + path;

                if (path.EndsWith("{**catch-all}"))
                {
                    path = path.Replace("{**catch-all}", "{rest}");
                }

                if (!swaggerDoc.Paths.ContainsKey($"/{path}"))
                {
                    var pathItem = new OpenApiPathItem
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Get] = CreateOperation(route.RouteId),
                            [OperationType.Post] = CreateOperation(route.RouteId),
                            [OperationType.Put] = CreateOperation(route.RouteId),
                            [OperationType.Delete] = CreateOperation(route.RouteId)
                        }
                    };

                    swaggerDoc.Paths.Add($"/{path}", pathItem);
                }
            }
        }

        private OpenApiOperation CreateOperation(string routeId)
        {
            return new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new() { Name = routeId } },
                Parameters = new List<OpenApiParameter>
            {
                new()
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" }
                }
            },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "Success" },
                    ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                    ["403"] = new OpenApiResponse { Description = "Forbidden" }
                }
            };
        }
    }
}
