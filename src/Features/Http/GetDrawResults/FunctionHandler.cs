using System.Globalization;
using Lotto.Draws;
using Microsoft.Extensions.Caching.Memory;

namespace Lotto.Features.Http.GetDrawResults;

internal sealed record Request(DateOnly? DateFrom, DateOnly? DateTo, int? Top);

internal sealed class FunctionHandler(
    IDrawResultsRepository repository,
    IMemoryCache cache,
    ILogger<FunctionHandler> logger)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<IEnumerable<DrawResults>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var (dateFrom, dateTo, top) = request;

        logger.LogInformation(
            "Handling GetDrawResult - DateFrom: {DateFrom}, DateTo: {DateTo}, Top: {Top}",
            dateFrom, dateTo, top);

        var cacheKey = BuildCacheKey(request);

        if (cache.TryGetValue(cacheKey, out DrawResults[]? cachedResults) && cachedResults is not null)
        {
            logger.LogInformation("Returning {Count} cached draw results.", cachedResults.Length);
            return cachedResults;
        }

        logger.LogInformation("Fetching results from storage...");

        var resultsTopValue = top ?? int.MaxValue;
        var drawResults = (await repository.GetAsync(dateFrom, dateTo, resultsTopValue, cancellationToken)).ToArray();

        cache.Set(cacheKey, drawResults, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        if (drawResults.Length == 0)
        {
            logger.LogWarning("No draw results found for the given filter.");
            return [];
        }

        logger.LogInformation("Handled GetDrawResult. Successfully retrieved {Count} results.", drawResults.Length);

        return drawResults;
    }

    private static string BuildCacheKey(Request request)
    {
        var dateFrom = request.DateFrom?.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture) ?? "none";
        var dateTo = request.DateTo?.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture) ?? "none";
        var top = request.Top?.ToString(CultureInfo.InvariantCulture) ?? "all";
        return $"GetDrawResults:{dateFrom}:{dateTo}:{top}";
    }

}
