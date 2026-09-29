using Lotto.Draws;

namespace Lotto.Interfaces;

internal interface IDrawResultsRepository
{
    Task<IEnumerable<DrawResults>> GetAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int top,
        CancellationToken cancellationToken);

    Task<DrawResults?> GetLatestAsync(CancellationToken cancellationToken);

    Task AddAsync(DrawResults data, CancellationToken cancellationToken);
}
