using System.Net;
using System.Text.Json;
using Lotto.Attributes;
using Lotto.Draws;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetLatestDrawResults;

internal sealed class HttpGetFunction(
    IDrawResultsRepository repository,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<HttpGetFunction> logger)
{
    private const string FunctionName = "GetLatestDrawResults";

    [Function(FunctionName)]
    [Operation(FunctionName, "Draw Results")]
    [JsonResponse(HttpStatusCode.OK, typeof(DrawResultsDto))]
    [JsonResponse(HttpStatusCode.NotFound, typeof(string))]
    [FunctionKeySecurity]
    public async Task<IActionResult> Run(
        [HttpTrigger("get", Route = "draw-results/latest")] HttpRequest _,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{FunctionName} handling request.", FunctionName);
        var response = await HandleRequestAsync(cancellationToken);
        logger.LogInformation("{FunctionName} finished successfully.", FunctionName);
        return response;
    }

    private async Task<IActionResult> HandleRequestAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching draw results...");

        var result = await repository.GetLatestAsync(cancellationToken);

        if (result is null)
        {
            logger.LogWarning("No draw results found in storage.");
            return new NotFoundObjectResult("No draw results found.");
        }

        var dto = result.ToDrawResultsDto();

        return new ContentResult
        {
            Content = JsonSerializer.Serialize(dto, jsonSerializerOptions),
            ContentType = "application/json",
            StatusCode = 200
        };
    }
}
