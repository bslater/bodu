// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyOfTCurrencyJsonConverterTests
{

    /// <summary>
    /// Verifies that a currency field that disagrees with the type parameter is rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Strict_WhenCurrencyMismatchesTypeParameter_ShouldThrowJsonException() =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1,\"currency\":\"EUR\"}", OptionsFor(FinancialJsonPolicy.Strict)));

    /// <summary>
    /// Verifies that malformed payloads — a non-numeric amount or a missing property — are rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("null", DisplayName = "Top-level null")]
    [DataRow("[]", DisplayName = "Wrong root shape — array")]
    [DataRow("{}", DisplayName = "Empty object")]
    [DataRow("{\"amount\":19.99}", DisplayName = "Missing currency")]
    [DataRow("{\"currency\":\"USD\"}", DisplayName = "Missing amount")]
    [DataRow("{\"amount\":null,\"currency\":\"USD\"}", DisplayName = "Amount is null")]
    [DataRow("{\"amount\":true,\"currency\":\"USD\"}", DisplayName = "Amount is boolean")]
    [DataRow("{\"amount\":{},\"currency\":\"USD\"}", DisplayName = "Amount is object")]
    [DataRow("{\"amount\":[],\"currency\":\"USD\"}", DisplayName = "Amount is array")]
    [DataRow("{\"amount\":\"not-a-number\",\"currency\":\"USD\"}", DisplayName = "Amount is non-numeric string")]
    [DataRow("{\"amount\":19.99,\"currency\":null}", DisplayName = "Currency is null")]
    [DataRow("{\"amount\":19.99,\"currency\":123}", DisplayName = "Currency is a number")]
    [DataRow("{\"amount\":19.99,\"currency\":\"usd\"}", DisplayName = "Currency case mismatch")]
    [DataRow("{\"amount\":19.99,\"currency\":\" USD \"}", DisplayName = "Currency with surrounding whitespace")]
    public void Strict_WhenPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money<USD>>(json, OptionsFor(FinancialJsonPolicy.Strict)));

    /// <summary>
    /// Verifies that a JSON payload with duplicate <c>"amount"</c> or <c>"currency"</c> properties is rejected —
    /// last-write-wins on financial payloads is a silent data-integrity hazard.
    /// </summary>
    [TestMethod]
    [DataRow("{\"amount\":10,\"amount\":20,\"currency\":\"USD\"}", DisplayName = "Duplicate amount")]
    [DataRow("{\"amount\":10,\"currency\":\"USD\",\"currency\":\"JPY\"}", DisplayName = "Duplicate currency")]
    public void Strict_WhenPropertyDuplicated_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money<USD>>(json, OptionsFor(FinancialJsonPolicy.Strict)));

    /// <summary>
    /// Verifies that the reader accepts a numeric <c>"amount"</c> emitted as a JSON string — documented lenient
    /// behaviour for systems lacking arbitrary-precision number support.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAmountIsNumericString_ShouldAcceptIt()
    {
        Money<USD> money = JsonSerializer.Deserialize<Money<USD>>("{\"amount\":\"19.99\",\"currency\":\"USD\"}", OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(new Money<USD>(19.99m), money);
    }

    /// <summary>
    /// Verifies that an over-precision input is interpreted at the currency's precision, matching the documented
    /// settlement-value contract.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAmountHasOverPrecision_ShouldNormaliseToMinorUnits()
    {
        Money<USD> money = JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1.999,\"currency\":\"USD\"}", OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(new Money<USD>(2.00m), money);
    }

    /// <summary>
    /// Verifies that writing a value whose currency tag reports invalid metadata throws
    /// <see cref="InvalidOperationException" /> rather than emitting a corrupt payload.
    /// </summary>
    [TestMethod]
    public void Strict_WhenCurrencyMetadataInvalid_ShouldThrowInvalidOperationExceptionOnWrite()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = JsonSerializer.Serialize(default(Money<NullIsoCurrency>), OptionsFor(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// A custom currency tag that reports a null ISO code — invalid metadata for the write path.
    /// </summary>
    private sealed class NullIsoCurrency
        : ICurrency
    {
        public static string IsoCode => null!;
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}" /> serializes to a JSON object with <c>"amount"</c> and
    /// <c>"currency"</c> properties.
    /// </summary>
    [TestMethod]
    public void Strict_WhenSerializing_ShouldEmitAmountAndCurrencyFields()
    {
        string json = JsonSerializer.Serialize(new Money<USD>(19.99m), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that deserialization accepts property names in any casing.
    /// </summary>
    [TestMethod]
    public void Strict_WhenPropertyNamesCasedDifferently_ShouldStillSucceed()
    {
        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>("{\"Amount\":19.99,\"Currency\":\"USD\"}", OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }
}
