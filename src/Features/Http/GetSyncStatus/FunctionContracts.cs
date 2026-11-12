namespace Lotto.Features.Http.GetSyncStatus;

internal sealed record SyncDto(
    string? LatestSyncDate,
    string LatestDrawDate,
    bool IsUpToDate);
