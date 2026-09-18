namespace FintechPlatform.Api.Infrastructure;

public static class MoneyRules
{
    public static decimal RequireValidAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Amount supports at most two decimal places.", nameof(amount));

        return amount;
    }

    public static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(currency));

        return currency.Trim().ToUpperInvariant();
    }
}
