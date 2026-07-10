// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.Compact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterTests
{

    /// <summary>
    /// Verifies that the Compact policy round-trips through the <c>"amount ISO"</c> string form.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRoundTripped_ShouldPreserveValue()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreEqual("\"19.99 USD\"", json);
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), JsonSerializer.Deserialize<Money>(json, options));
    }

    /// <summary>
    /// Verifies that the Compact policy rejects a non-string token and an unparseable string.
    /// </summary>
    [TestMethod]
    [DataRow("123", DisplayName = "Compact token is a number")]
    [DataRow("\"not money\"", DisplayName = "Compact string is not money")]
    public void Compact_WhenPayloadInvalid_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Compact)));
}
