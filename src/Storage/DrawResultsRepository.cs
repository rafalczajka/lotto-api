using System.Globalization;
using Azure.Data.Tables;
using Lotto.Draws;
using Lotto.Storage.Entities;

namespace Lotto.Storage;

internal sealed class DrawResultsRepository(
    TableServiceClient tableServiceClient,
    IRowKeyGenerator rowKeyGenerator,
    IOptions<TableOptions> tableOptions) : IDrawResultsRepository
{
    private const string PartitionKey = "LottoData";
    private const string BaseFilter = $"PartitionKey eq '{PartitionKey}'";
    private const int MaxPageSize = 1_000;
    private readonly string _tableName = tableOptions.Value.DrawResultsTableName;

    public async Task<IEnumerable<DrawResultsEntity>> GetAsync(string filter, int top, CancellationToken cancellationToken)
    {
        var results = new List<DrawResultsEntity>();

        var fullFilter = !string.IsNullOrWhiteSpace(filter) ? $"{BaseFilter} and ({filter})" : BaseFilter;
        var client = tableServiceClient.GetTableClient(_tableName);
        var query = client.QueryAsync<DrawResultsEntity>(fullFilter, cancellationToken: cancellationToken);
        var pageSize = Math.Min(MaxPageSize, top);

        await foreach (var page in query.AsPages(pageSizeHint: pageSize).WithCancellation(cancellationToken))
        {
            var remaining = top - results.Count;

            results.AddRange(
                remaining < page.Values.Count
                    ? page.Values.Take(remaining)
                    : page.Values);

            if (results.Count >= top) break;
        }

        return results;
    }

    public async Task<DrawResultsEntity> GetLatestAsync(CancellationToken cancellationToken)
    {
        var client = tableServiceClient.GetTableClient(_tableName);

        var entity = await client
            .QueryAsync<DrawResultsEntity>(BaseFilter, maxPerPage: 1, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        return entity ?? throw new InvalidOperationException("No DrawResults found");
    }

    public async Task AddAsync(DrawResults data, CancellationToken cancellationToken)
    {
        var rowKey = rowKeyGenerator.GenerateRowKey(data.DrawDate);

        var entity = new DrawResultsEntity
        {
            PartitionKey = PartitionKey,
            RowKey = rowKey,
            DrawDate = data.DrawDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
            LottoNumbers = data.LottoNumbersString,
            PlusNumbers = data.PlusNumbersString
        };

        var client = tableServiceClient.GetTableClient(_tableName);
        await client.AddEntityAsync(entity, cancellationToken);
    }
}
