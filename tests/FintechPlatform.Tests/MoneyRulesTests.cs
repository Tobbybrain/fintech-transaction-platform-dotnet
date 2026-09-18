using Xunit;
using FintechPlatform.Api.Infrastructure;

namespace FintechPlatform.Tests;

public sealed class MoneyRulesTests
{
    [Theory]
    [InlineData("1.00")]
    [InlineData("250000.99")]
    [InlineData("0.01")]
    public void Valid_amounts_are_accepted(string raw)
    {
        var amount = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(amount, MoneyRules.RequireValidAmount(amount));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Non_positive_amounts_are_rejected(string raw)
    {
        var amount = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentOutOfRangeException>(() => MoneyRules.RequireValidAmount(amount));
    }

    [Fact]
    public void More_than_two_decimal_places_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => MoneyRules.RequireValidAmount(10.001m));
    }
}
