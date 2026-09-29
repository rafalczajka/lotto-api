using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using Lotto.Draws;
using Microsoft.AspNetCore.Mvc;

namespace Lotto.Features.Http.GetDrawResults;

internal sealed class ResponseHandler(ILogger<ResponseHandler> logger)
{
    public async Task<IActionResult> HandleAsync(
        IList<DrawResults> results,
        ContentType contentType,
        CancellationToken cancellationToken)
    {
        if (results.Count != 0)
        {
            return contentType switch
            {
                ContentType.ApplicationJson => CreateJsonResponse(results),
                ContentType.ApplicationOctetStream => await CreateCsvResponseAsync(results, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(contentType))
            };
        }

        logger.LogWarning("No results found for the given query parameters.");
        return new NotFoundObjectResult("No historical draw results found.");
    }

    private IActionResult CreateJsonResponse(IList<DrawResults> data)
    {
        var dto = data.Select(r => r.ToDrawResultsDto()).ToList();
        logger.LogInformation("Successfully retrieved {ResultCount} results. Sending JSON response...", dto.Count);
        return new OkObjectResult(dto);
    }

    private async Task<IActionResult> CreateCsvResponseAsync(
        IList<DrawResults> data,
        CancellationToken cancellationToken)
    {
        var records = data
            .Select(r => new DrawResultsCsvRecord(
                r.DrawDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
                string.Join(",", r.LottoNumbers),
                string.Join(",", r.PlusNumbers)))
            .ToList();

        logger.LogInformation("Successfully retrieved {ResultCount} results. Creating a CSV file...", records.Count);

        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(records, cancellationToken);

        return new FileContentResult(Encoding.UTF8.GetBytes(writer.ToString()), "application/octet-stream")
        {
            FileDownloadName = $"lotto-export_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv"
        };
    }
}
