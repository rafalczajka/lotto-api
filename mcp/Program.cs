using System.Net.Http.Headers;
using Lotto.MCP;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    const string baseUrlPropertyName = "ApiBaseUrl";
    const string apiKeyPropertyName = "ApiKey";

    var baseUrlConfigValue = builder.Configuration[baseUrlPropertyName];
    var apiKeyConfigValue = builder.Configuration[apiKeyPropertyName];

    var baseUrl = !string.IsNullOrWhiteSpace(baseUrlConfigValue)
        ? baseUrlConfigValue
        : throw new InvalidOperationException($"'{baseUrlPropertyName}' missing in the configuration.");
    var apiKey = !string.IsNullOrWhiteSpace(apiKeyConfigValue)
        ? apiKeyConfigValue
        : throw new InvalidOperationException($"'{apiKeyPropertyName}' missing in the configuration.");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("x-functions-key", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Build().Run();
