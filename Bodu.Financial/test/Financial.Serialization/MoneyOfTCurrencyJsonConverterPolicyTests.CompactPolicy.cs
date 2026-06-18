// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.CompactPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class MoneyOfTCurrencyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Compact" /> writes the <c>"&lt;amount&gt; &lt;ISO&gt;"</c>
    /// string form for <see cref="Money{TCurrency}" />.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingMoney_ShouldEmitAmountSpaceIsoString()
    {
        var money = new Money<USD>(19.99m);

        string json = JsonSerializer.Serialize(money, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"19.99 USD\"", json);
    }

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Compact" /> round-trips a <see cref="Money{TCurrency}" />
    /// through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTrippingMoney_ShouldPreserveValue()
    {
        var original = new Money<USD>(1234.56m);
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        Money<USD> recovered = JsonSerializer.Deserialize<Money<USD>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that compact read accepts the <c>"&lt;ISO&gt; &lt;amount&gt;"</c> prefix arrangement that
    /// <see cref="Money{TCurrency}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money{TCurrency})" /> already
    /// supports.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingMoneyWithIsoPrefix_ShouldSucceed()
    {
        string json = "\"USD 19.99\"";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that the JPY compact write emits zero fractional digits, honouring the currency's minor-unit
    /// precision.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingJpy_ShouldEmitNoFractionalDigits()
    {
        var money = new Money<JPY>(2000m);

        string json = JsonSerializer.Serialize(money, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"2000 JPY\"", json);
    }

    /// <summary>
    /// Verifies that the BHD compact write emits three fractional digits.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingBhd_ShouldEmitThreeFractionalDigits()
    {
        var money = new Money<BHD>(12.345m);

        string json = JsonSerializer.Serialize(money, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"12.345 BHD\"", json);
    }

    /// <summary>
    /// Verifies that compact reads reject a JSON object (the strict / lenient form).
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"USD\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that compact reads reject a string whose ISO code does not match <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingMismatchedCurrency_ShouldThrowJsonException()
    {
        string json = "\"19.99 JPY\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that compact reads reject a non-string token.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingNumericToken_ShouldThrowJsonException()
    {
        string json = "19.99";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Compact));
        });
    }
}
