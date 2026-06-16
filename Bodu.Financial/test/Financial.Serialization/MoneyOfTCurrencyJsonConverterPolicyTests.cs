// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> honours each <see cref="FinancialJsonPolicy" /> on
/// both the read and write paths.
/// </summary>
[TestClass]
public class MoneyOfTCurrencyJsonConverterPolicyTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the financial converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    /// <summary>
    /// Verifies that the attribute-driven default serializer still emits the canonical object form, matching the
    /// existing v1.0 contract.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var money = new Money<USD>(19.99m);

        string json = JsonSerializer.Serialize(money);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that the shipped <c>[JsonConverter]</c> attribute remains present so default (no-options)
    /// serialization picks the strict shape with no consumer configuration.
    /// </summary>
    [TestMethod]
    public void MoneyTypes_WhenInspected_ShouldStillDeclareJsonConverterAttribute()
    {
        Assert.IsTrue(typeof(Money<USD>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(Money).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(MoneyBag).IsDefined(typeof(JsonConverterAttribute), inherit: false));
    }

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

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Lenient" /> normalises lowercase ISO codes during read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Lenient" /> trims whitespace around ISO codes during read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingPaddedCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"  USD  \"}";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that the strict policy continues to reject lowercase ISO codes — i.e. lenient adds tolerance
    /// without back-porting it onto Strict.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
