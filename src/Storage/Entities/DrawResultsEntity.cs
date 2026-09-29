using Azure;
using System.Globalization;
using Azure.Data.Tables;
using Lotto.Draws;

namespace Lotto.Storage.Entities;

internal sealed class DrawResultsEntity : ITableEntity
{
    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public required string DrawDate { get; init; }

    public required string LottoNumbers { get; init; }

    public string? PlusNumbers { get; init; }
}

internal static class DrawResultsEntityExtensions
{
    public static DrawResults ToDrawResults(this DrawResultsEntity entity)
    {
        return new DrawResults
        {
            DrawDate = DateOnly.ParseExact(entity.DrawDate, Defaults.DateFormat, CultureInfo.InvariantCulture),
            LottoNumbers = entity.LottoNumbers.Split(',').Select(int.Parse),
            PlusNumbers = !string.IsNullOrWhiteSpace(entity.PlusNumbers)
                ? entity.PlusNumbers.Split(',').Select(int.Parse)
                : [],
            LottoNumbersString = entity.LottoNumbers,
            PlusNumbersString = entity.PlusNumbers
        };
    }
}
