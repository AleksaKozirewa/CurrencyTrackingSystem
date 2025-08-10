using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Configuration;

namespace CurrencyTrackingSystem.API.Filters
{
    public class YarpSwaggerFilter : IDocumentFilter
    {
        private readonly IProxyConfigProvider _proxyConfig;
        private readonly ILogger<YarpSwaggerFilter> _logger;

        public YarpSwaggerFilter(IProxyConfigProvider proxyConfig, ILogger<YarpSwaggerFilter> logger)
        {
            _proxyConfig = proxyConfig;
            _logger = logger;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var proxyConfig = _proxyConfig.GetConfig();
            var addedPaths = new List<string>();

            foreach (var route in proxyConfig.Routes)
            {
                try
                {
                    var path = NormalizePath(route.Match.Path);
                    if (string.IsNullOrEmpty(path))
                    {
                        _logger.LogWarning("Empty path for route {RouteId}", route.RouteId);
                        continue;
                    }

                    var methods = GetOperationMethods(route);
                    if (methods.Count == 0)
                    {
                        _logger.LogWarning("No methods defined for route {RouteId}", route.RouteId);
                        continue;
                    }

                    if (!swaggerDoc.Paths.TryGetValue(path, out var pathItem))
                    {
                        pathItem = new OpenApiPathItem();
                        swaggerDoc.Paths.Add(path, pathItem);
                        addedPaths.Add(path);
                    }

                    foreach (var method in methods)
                    {
                        if (!pathItem.Operations.ContainsKey(method))
                        {
                            pathItem.Operations[method] = CreateOperation(route, method);
                            _logger.LogInformation("Added {Method} operation for {Path}", method, path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing route {RouteId}", route.RouteId);
                }
            }

            _logger.LogInformation("Total paths added: {Count}", addedPaths.Count);
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Удаляем возможные префиксы
            path = path.TrimStart('~').TrimStart('/');

            // Обработка catch-all параметров
            path = path.Replace("{**catch-all}", "{any}")
                      .Replace("{**rest}", "{any}");

            // Добавляем /api префикс если его нет (согласно вашей конфигурации)
            if (!path.StartsWith("api/") && !path.StartsWith("/api/"))
            {
                path = $"api/{path}";
            }

            return $"/{path.TrimStart('/')}";
        }

        private List<OperationType> GetOperationMethods(RouteConfig route)
        {
            var methods = new List<OperationType>();

            if (route.Match.Methods == null || !route.Match.Methods.Any())
            {
                return methods;
            }

            foreach (var method in route.Match.Methods)
            {
                switch (method.ToUpper())
                {
                    case "GET":
                        methods.Add(OperationType.Get);
                        break;
                    case "POST":
                        methods.Add(OperationType.Post);
                        break;
                    case "PUT":
                        methods.Add(OperationType.Put);
                        break;
                    case "DELETE":
                        methods.Add(OperationType.Delete);
                        break;
                }
            }

            return methods;
        }

        private OpenApiOperation CreateOperation(RouteConfig route, OperationType operationType)
        {
            var operation = new OpenApiOperation
            {
                OperationId = $"{route.RouteId}_{operationType}",
                Tags = new List<OpenApiTag> { new() { Name = route.ClusterId } },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "Success" },
                    ["400"] = new OpenApiResponse { Description = "Bad Request" },
                    ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                    ["403"] = new OpenApiResponse { Description = "Forbidden" },
                    ["415"] = new OpenApiResponse { Description = "Unsupported Media Type" }
                }
            };

            // Добавляем параметры авторизации если требуется
            if (!string.IsNullOrEmpty(route.AuthorizationPolicy))
            {
                operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    }] = new List<string>()
                }
            };
            }

            // Добавляем RequestBody для POST/PUT методов
            if (operationType == OperationType.Post || operationType == OperationType.Put)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = "object" },
                            Example = CreateJsonExample(route.RouteId, operationType)
                        }
                    }
                };
            }

            return operation;
        }

        private IOpenApiAny CreateJsonExample(string routeId, OperationType operationType)
        {
            return operationType switch
            {
                OperationType.Post when routeId.Contains("register") =>
                    OpenApiAnyFactory.CreateFromJson("""
                    {
                        "username": "string",
                        "password": "string"
                    }
                    """),

                OperationType.Post when routeId.Contains("login") =>
                    OpenApiAnyFactory.CreateFromJson("""
                    {
                        "username": "string",
                        "password": "string"
                    }
                    """),

                OperationType.Post when routeId.Contains("logout") =>
                    OpenApiAnyFactory.CreateFromJson(string.Empty),

                OperationType.Put when routeId.Contains("favorites") =>
                    OpenApiAnyFactory.CreateFromJson("""
                    {
                        "currencyIds": [
                      "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    ]
                    }
                    """),

                    _ => OpenApiAnyFactory.CreateFromJson("""
                    {
                        "property1": "value1",
                        "property2": 123
                    }
                    """)
            };
        }
    }
}
