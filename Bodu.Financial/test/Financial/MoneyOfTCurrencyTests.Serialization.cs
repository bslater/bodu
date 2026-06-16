// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}" /> serializes to a JSON object with <c>"amount"</c> and
    /// <c>"currency"</c> properties.
    /// </summary>
    [TestMethod]
    public void JsonSerialize_WhenCalled_ShouldEmitAmountAndCurrencyFields()
    {
        var money = new Money<USD>(19.99m);

        string json = JsonSerializer.Serialize(money);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that JSON round-tripping preserves the value.
    /// </summary>
    [TestMethod]
    public void JsonRoundTrip_WhenCalled_ShouldPreserveValue()
    {
        var original = new Money<USD>(123.45m);

        string json = JsonSerializer.Serialize(original);
        Money<USD> recovered = JsonSerializer.Deserialize<Money<USD>>(json);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that deserialization rejects a payload whose currency code does not match
    /// <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenCurrencyMismatch_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        });
    }

    /// <summary>
    /// Verifies that deserialization throws when the payload is missing the required <c>"amount"</c> property.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenAmountMissing_ShouldThrowJsonException()
    {
        string json = "{\"currency\":\"USD\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        });
    }

    /// <summary>
    /// Verifies that deserialization throws when the payload is missing the required <c>"currency"</c> property.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenCurrencyMissing_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        });
    }

    /// <summary>
    /// Verifies that JSON deserialization accepts property names in any casing.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenPropertyNamesCasedDifferently_ShouldStillSucceed()
    {
        string json = "{\"Amount\":19.99,\"Currency\":\"USD\"}";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json);

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }
}
