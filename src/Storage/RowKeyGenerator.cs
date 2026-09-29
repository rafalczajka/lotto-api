using System.Globalization;

namespace Lotto.Storage;

internal sealed class RowKeyGenerator : IRowKeyGenerator
{
    public string GenerateRowKey(DateOnly date)
    {
        var reversedDate = DateOnly.FromDayNumber(DateOnly.MaxValue.DayNumber - date.DayNumber);
        return reversedDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }
}
