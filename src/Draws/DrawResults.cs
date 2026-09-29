namespace Lotto.Draws;

internal sealed class DrawResults
{
    public required DateOnly DrawDate { get; init; }

    public required IReadOnlyList<int> LottoNumbers { get; init; }

    public required IReadOnlyList<int> PlusNumbers { get; init; }
}
