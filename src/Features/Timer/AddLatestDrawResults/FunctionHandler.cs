using Lotto.Draws;

namespace Lotto.Features.Timer.AddLatestDrawResults;

internal sealed class FunctionHandler(
    IDrawResultsRepository repository,
    LottoClient lottoClient,
    ILogger<FunctionHandler> logger)
{
    private const string FunctionName = "AddLatestDrawResults";

    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{FunctionName} handler started.", FunctionName);

        var getDataFromStorageTask = repository.GetLatestAsync(cancellationToken);
        var getDataFromApiTask = lottoClient.GetLatestDrawResultsAsync(cancellationToken);

        var storageData = await getDataFromStorageTask;
        var apiData = await getDataFromApiTask;
        logger.LogInformation(
            "{FunctionName} comparing draw dates: storage={StorageDate}, api={ApiDate}",
            FunctionName, storageData.DrawDate, apiData.DrawDate);

        if (storageData.DrawDate == apiData.DrawDate)
        {
            logger.LogWarning("{FunctionName} skipped: storage already has the latest draw results.", FunctionName);
            return;
        }

        logger.LogInformation(
            "{FunctionName} persisting new draw results for {DrawDate}",
            FunctionName, apiData.DrawDate);

        await repository.AddAsync(apiData, cancellationToken);

        logger.LogInformation("{FunctionName} handler finished successfully.", FunctionName);
    }
}
