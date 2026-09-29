namespace Lotto.Draws;

internal sealed class DrawResults
{
    public required DateOnly DrawDate { get; init; }

    public required IEnumerable<int> LottoNumbers { get; init; }

    public required IEnumerable<int> PlusNumbers { get; init; }

    public required string LottoNumbersString { get; init; }

    public string? PlusNumbersString { get; init; }
}
