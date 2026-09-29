using System.Globalization;
using Azure;
using Azure.Data.Tables;
using Lotto.Storage.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Lotto.Storage;

internal sealed class DrawResultsSeeder(
    IWebHostEnvironment env,
    TableServiceClient tableServiceClient,
    IRowKeyGenerator rowKeyGenerator,
    IOptions<TableOptions> tableOptions) : IHostedService
{
    private const int SeedDateRangeInYears = 10;
    private const int MaxTransactionSize = 100;
    private const string PartitionKey = "LottoData";

    private static readonly TimeZoneInfo DrawTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    private readonly string _tableName = tableOptions.Value.DrawResultsTableName;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment()) return;

        var tableClient = tableServiceClient.GetTableClient(_tableName);
        var tableCreated = await TryCreateTableAsync(tableClient, cancellationToken);

        if (!tableCreated) return;

        var createdAt = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(createdAt, DrawTimeZone).DateTime);
        var draws = GenerateDrawSchedule(today.AddYears(-SeedDateRangeInYears), today);
        var batch = new List<TableTransactionAction>(MaxTransactionSize);

        foreach (var drawDate in draws)
        {
            var random = new Random(drawDate.DayNumber);
            var lotto = GenerateNumbers(random);
            var plus = GenerateNumbers(random);

            var entity = new DrawResultsEntity
            {
                PartitionKey = PartitionKey,
                RowKey = rowKeyGenerator.GenerateRowKey(drawDate),
                Timestamp = createdAt,
                DrawDate = drawDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
                LottoNumbers = string.Join(",", lotto),
                PlusNumbers = string.Join(",", plus)
            };

            batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity));

            if (batch.Count != MaxTransactionSize) continue;

            await tableClient.SubmitTransactionAsync(batch, cancellationToken);
            batch.Clear();
        }

        if (batch.Count > 0) await tableClient.SubmitTransactionAsync(batch, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<bool> TryCreateTableAsync(TableClient tableClient, CancellationToken cancellationToken)
    {
        try
        {
            await tableClient.CreateAsync(cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return false;
        }
    }

    private static IEnumerable<DateOnly> GenerateDrawSchedule(DateOnly startDate, DateOnly endDate)
    {
        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Saturday)
                yield return date;
        }
    }

    private static IEnumerable<int> GenerateNumbers(Random random)
    {
        const int numbersInSingleDraw = 6;
        var set = new HashSet<int>();

        while (set.Count < numbersInSingleDraw)
            set.Add(random.Next(1, 50));

        return set.Order();
    }
}
