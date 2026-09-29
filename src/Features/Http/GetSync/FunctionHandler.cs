using System.Globalization;
using Lotto.Draws;

namespace Lotto.Features.Http.GetSync;

internal sealed class FunctionHandler(
    IDrawResultsRepository repository,
    LottoClient lottoClient,
    ILogger<FunctionHandler> logger)
{
    public async Task<SyncDto> HandleAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSync.");

        DateOnly? storageDate = null;

        try
        {
            storageDate = (await repository.GetLatestAsync(cancellationToken)).DrawDate;
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("No draw results found in storage.");
        }

        var apiDate = (await lottoClient.GetLatestDrawResultsAsync(cancellationToken)).DrawDate;
        var isUpToDate = storageDate == apiDate;

        logger.LogInformation(
            "Sync status - StorageDate: {StorageDate}, ApiDate: {ApiDate}, UpToDate: {UpToDate}",
            storageDate, apiDate, isUpToDate);

        return new SyncDto(
            storageDate?.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
            apiDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
            isUpToDate);
    }
}
