using System.Net;
using Lotto.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetLatestDrawResults;

internal sealed class HttpGetFunction(
    IDrawResultsRepository repository,
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

        if (result is not null) return new OkObjectResult(result.ToDrawResultsDto());

        logger.LogWarning("No draw results found in storage.");
        return new NotFoundObjectResult("No draw results found.");
    }
}
