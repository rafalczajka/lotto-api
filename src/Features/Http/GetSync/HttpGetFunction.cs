using System.Net;
using System.Net.Http;
using Lotto.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetSync;

internal sealed class HttpGetFunction(
    FunctionHandler handler,
    ILogger<HttpGetFunction> logger)
{
    private const string FunctionName = "GetSync";

    [Function(FunctionName)]
    [Operation(FunctionName, "Sync")]
    [JsonResponse(HttpStatusCode.OK, typeof(SyncDto))]
    [JsonResponse(HttpStatusCode.BadGateway, typeof(ErrorResponse))]
    [JsonResponse(HttpStatusCode.InternalServerError, typeof(ErrorResponse))]
    [FunctionKeySecurity]
    public async Task<IActionResult> Run(
        [HttpTrigger("get", Route = "sync")] HttpRequest _,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("{FunctionName} handling request.", FunctionName);
            var status = await handler.HandleAsync(cancellationToken);
            logger.LogInformation("{FunctionName} finished successfully.", FunctionName);

            return new OkObjectResult(status);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "{FunctionName} failed to fetch data from API.", FunctionName);

            return new ObjectResult(new ErrorResponse("Failed to retrieve data from external API."))
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }
}
