namespace Lotto.Features.Http.GetDrawResults;

[CsvHelper.CsvRecord]
internal sealed partial record DrawResultsCsvRecord(string DrawDate, string LottoNumbers, string? PlusNumbers);
