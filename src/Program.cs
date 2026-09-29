using System.Text.Json;
using Lotto;
using Lotto.Draws;
using Lotto.Storage;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddHttpClient<LottoClient>(client =>
{
    const string baseUrlPropertyName = "LottoBaseUrl";
    const string apiKeyPropertyName = "LottoApiKey";

    var baseUrlConfigValue = builder.Configuration[baseUrlPropertyName];
    var apiKeyConfigValue = builder.Configuration[apiKeyPropertyName];

    var baseUrl = !string.IsNullOrWhiteSpace(baseUrlConfigValue)
        ? baseUrlConfigValue
        : throw new InvalidOperationException($"'{baseUrlPropertyName}' missing in the configuration.");
    var apiKey = !string.IsNullOrWhiteSpace(apiKeyConfigValue)
        ? apiKeyConfigValue
        : throw new InvalidOperationException($"'{apiKeyPropertyName}' missing in the configuration.");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("secret", apiKey);
});

builder.Services.AddStorage(builder.Configuration, builder.Environment);
builder.RunFeatureInstallers();

builder.Build().Run();
