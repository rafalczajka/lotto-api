namespace Lotto.Features.Timer.AddLatestDrawResults;

internal sealed class TimerFunction(FunctionHandler handler, ILogger<TimerFunction> logger)
{
    private const string FunctionName = "AddLatestDrawResults";

    [Function(FunctionName), FixedDelayRetry(3, "00:15:00")]
    public async Task Run(
        // Monitoring disabled intentionally to avoid storage writes.
        // Missed runs are handled by synchronization logic.
        [TimerTrigger("%DataSyncSchedule%", UseMonitor = false)] TimerInfo _,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{FunctionName} function triggered at: {TriggerTime}", FunctionName, DateTimeOffset.UtcNow);
        await handler.HandleAsync(cancellationToken);
        logger.LogInformation("{FunctionName} finished successfully.", FunctionName);
    }
}
