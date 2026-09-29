namespace Lotto.Interfaces;

internal interface IRowKeyGenerator
{
    string GenerateRowKey(DateOnly date);
}
