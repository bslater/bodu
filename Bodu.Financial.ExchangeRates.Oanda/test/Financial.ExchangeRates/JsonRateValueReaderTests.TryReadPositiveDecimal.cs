// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.TryReadPositiveDecimal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class JsonRateValueReaderTests
{
    /// <summary>
    /// Verifies that a positive JSON number, or a JSON string holding one, reads as that decimal.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("123.450")]
    [DataRow("\"123.450\"")]
    public void TryReadPositiveDecimal_WhenJsonIsPositiveNumberOrNumericString_ShouldReturnTheValue(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadPositiveDecimal(document.RootElement, out decimal rate);

        Assert.IsTrue(success);
        Assert.AreEqual(123.450m, rate);
    }

    /// <summary>
    /// Verifies that zero, a negative number, <c>null</c> and a non-numeric string are not read as a rate.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("-1.5")]
    [DataRow("null")]
    [DataRow("\"invalid\"")]
    public void TryReadPositiveDecimal_WhenJsonIsNotPositiveDecimal_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.IsFalse(JsonRateValueReader.TryReadPositiveDecimal(document.RootElement, out _));
    }
}
