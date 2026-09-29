using System.Globalization;
using System.Net;
using Lotto.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetDrawResultsByDate;

internal sealed class HttpGetFunction(
    FunctionHandler handler,
    ILogger<HttpGetFunction> logger)
{
    private const string FunctionName = "GetDrawResultsByDate";

    [Function(FunctionName)]
    [Operation(FunctionName, "Draw Results")]
    [PathParam("date")]
    [JsonResponse(HttpStatusCode.OK, typeof(DrawResultsDto))]
    [JsonResponse(HttpStatusCode.BadRequest, typeof(ErrorResponse))]
    [JsonResponse(HttpStatusCode.NotFound, typeof(string))]
    [FunctionKeySecurity]
    public async Task<IActionResult> Run(
        [HttpTrigger("get", Route = "draw-results/{date:datetime}")] HttpRequest _,
        string date,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{FunctionName} handling request for date {Date}", FunctionName, date);
        var response = await HandleRequestAsync(date, cancellationToken);
        logger.LogInformation("{FunctionName} finished successfully.", FunctionName);
        return response;
    }

    private async Task<IActionResult> HandleRequestAsync(string date, CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate, out var errorMessage))
        {
            logger.LogError("Route date validation failed: {ErrorMessage}", errorMessage);
            return new BadRequestObjectResult(new ErrorResponse(errorMessage!));
        }

        var result = await handler.HandleAsync(parsedDate, cancellationToken);

        if (result is not null) return new OkObjectResult(result.ToDrawResultsDto());

        return new NotFoundObjectResult("No draw results found for the given date.");
    }

    private static bool TryParseDate(string date, out DateOnly parsedDate, out string? errorMessage)
    {
        errorMessage = null;

        var isValid = DateOnly.TryParseExact(
            date,
            Defaults.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedDate);

        if (!isValid) errorMessage = $"'date' must be a valid date in the format {Defaults.DateFormat}.";

        return isValid;
    }
}
