namespace BalanceService.Core;

public interface ICurrencyConverter
{
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
}