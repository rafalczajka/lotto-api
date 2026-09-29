using System.Globalization;
using Lotto.Draws;

namespace Lotto.Features.Http;

internal sealed record DrawResultsDto(
    string DrawDate,
    IEnumerable<int> LottoNumbers,
    IEnumerable<int> PlusNumbers);

internal sealed record ErrorResponse(
    string Error);

internal static class DrawResultsDtoExtensions
{
    public static DrawResultsDto ToDrawResultsDto(this DrawResults drawResults)
    {
        return new DrawResultsDto(
            drawResults.DrawDate.ToString(Defaults.DateFormat, CultureInfo.InvariantCulture),
            drawResults.LottoNumbers,
            drawResults.PlusNumbers);
    }
}
