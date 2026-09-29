using Lotto.Draws;

namespace Lotto.Features.Http.GetDrawResultsByDate;

internal sealed class FunctionHandler(IDrawResultsRepository repository, ILogger<FunctionHandler> logger)
{
    public async Task<DrawResults?> HandleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetDrawResultsByDate - Date: {Date}", date);

        var result = (await repository.GetAsync(date, date, 1, cancellationToken)).FirstOrDefault();

        if (result is not null) return result;

        logger.LogWarning("No draw results found for the given date.");
        return null;
    }
}
