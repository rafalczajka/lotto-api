using System.Net;
using Lotto.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetDrawResults;

internal sealed class HttpGetFunction(
    FunctionHandler handler,
    ResponseHandler responseHandler,
    IContentNegotiator<ContentType> contentNegotiator,
    ILogger<HttpGetFunction> logger)
{
    private const string FunctionName = "GetDrawResults";

    [Function(FunctionName)]
    [Operation(FunctionName, "Draw Results")]
    [QueryParam("dateFrom")]
    [QueryParam("dateTo")]
    [QueryParam("top", typeof(int))]
    [JsonResponse(HttpStatusCode.OK, typeof(DrawResultsDto[]))]
    [JsonResponse(HttpStatusCode.BadRequest, typeof(ErrorResponse))]
    [JsonResponse(HttpStatusCode.NotFound, typeof(string))]
    [JsonResponse(HttpStatusCode.NotAcceptable, typeof(ErrorResponse))]
    [FunctionKeySecurity]
    public async Task<IActionResult> Run(
        [HttpTrigger("get", Route = "draw-results")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "{FunctionName} handling request with query {QueryString} and accept {AcceptHeader}",
            FunctionName, request.QueryString, request.Headers.Accept.ToString());
        var response = await HandleRequestAsync(request, cancellationToken);
        logger.LogInformation("{FunctionName} finished successfully.", FunctionName);
        return response;
    }

    private async Task<IActionResult> HandleRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var queryParams = QueryParams.Parse(request.Query, out var errorMessage);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogError("Query parameters validation failed: {ErrorMessage}", errorMessage);
            return new BadRequestObjectResult(new ErrorResponse(errorMessage));
        }

        var (negotiationResult, contentType) = contentNegotiator.Negotiate(request);

        if (!negotiationResult)
            return HandleUnsupportedContentType(request);

        var (dateFrom, dateTo, top) = queryParams;
        var results = (await handler.HandleAsync(new Request(dateFrom, dateTo, top), cancellationToken)).ToList();
        return await responseHandler.HandleAsync(results, contentType, cancellationToken);
    }

    private ObjectResult HandleUnsupportedContentType(HttpRequest request)
    {
        var acceptHeader = request.Headers.Accept;
        logger.LogError("Unsupported 'Accept' header value: {AcceptHeader}", acceptHeader!);
        return new ObjectResult(new ErrorResponse($"Unsupported 'Accept' header value: {acceptHeader}"))
        {
            StatusCode = StatusCodes.Status406NotAcceptable
        };
    }
}
