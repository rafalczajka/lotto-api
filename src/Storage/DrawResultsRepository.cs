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

    public async Task<IEnumerable<DrawResults>> GetAsync(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int top,
        CancellationToken cancellationToken)
    {
        var client = tableServiceClient.GetTableClient(_tableName);

        var filter = BuildFilter(dateFrom, dateTo);
        var query = client.QueryAsync<DrawResultsEntity>(filter, cancellationToken: cancellationToken);

        var pageSize = Math.Min(MaxPageSize, top);
        var results = new List<DrawResults>();

        await foreach (var page in query.AsPages(pageSizeHint: pageSize).WithCancellation(cancellationToken))
        {
            var remaining = top - results.Count;
            results.AddRange(page.Values.Take(remaining).Select(Map));

            if (results.Count >= top) break;
        }

        return results;
    }

    public async Task<DrawResults> GetLatestAsync(CancellationToken cancellationToken)
    {
        var client = tableServiceClient.GetTableClient(_tableName);

        var entity = await client
            .QueryAsync<DrawResultsEntity>(BaseFilter, maxPerPage: 1, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null
            ? throw new InvalidOperationException("No DrawResults found")
            : Map(entity);
    }

    public async Task AddAsync(DrawResults data, CancellationToken cancellationToken)
    {
        var entity = new DrawResultsEntity
        {
            PartitionKey = PartitionKey,
            RowKey = rowKeyGenerator.GenerateRowKey(data.DrawDate),
            DrawDate = data.DrawDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
            LottoNumbers = string.Join(",", data.LottoNumbers),
            PlusNumbers = string.Join(",", data.PlusNumbers)
        };

        var client = tableServiceClient.GetTableClient(_tableName);
        await client.AddEntityAsync(entity, cancellationToken);
    }

    private static string BuildFilter(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var filters = new List<string> { BaseFilter };

        if (dateFrom is not null)
            filters.Add($"DrawDate ge '{dateFrom.Value.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture)}'");

        if (dateTo is not null)
            filters.Add($"DrawDate le '{dateTo.Value.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture)}'");

        return string.Join(" and ", filters);
    }

    private static DrawResults Map(DrawResultsEntity entity)
    {
        return new DrawResults
        {
            DrawDate = DateOnly.ParseExact(entity.DrawDate, Defaults.DateFormat, CultureInfo.InvariantCulture),
            LottoNumbers = ParseNumbers(entity.LottoNumbers),
            PlusNumbers = ParseNumbers(entity.PlusNumbers)
        };
    }

    private static int[] ParseNumbers(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [.. value.Split(',').Select(int.Parse)];
}
