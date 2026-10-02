namespace Lotto.MCP;

internal static class DateTimeOffsetExtensions
{
    public static DateOnly ToDateOnly(this DateTimeOffset date) => new(date.Year, date.Month, date.Day);

    public static DateOnly? ToDateOnly(this DateTimeOffset? date)
    {
        if (date is null) return null;
        return new DateOnly(date.Value.Year, date.Value.Month, date.Value.Day);
    }
}
