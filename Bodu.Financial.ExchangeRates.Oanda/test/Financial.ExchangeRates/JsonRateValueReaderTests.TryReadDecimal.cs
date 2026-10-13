// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.TryReadDecimal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class JsonRateValueReaderTests
{
    /// <summary>
    /// Verifies that a JSON number of any sign, or a JSON string holding one, reads as that decimal.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    /// <param name="expected">The expected value, as invariant text.</param>
    [TestMethod]
    [DataRow("123.450", "123.450")]
    [DataRow("\"123.450\"", "123.450")]
    [DataRow("-1.5", "-1.5")]
    [DataRow("\"-1.5\"", "-1.5")]
    [DataRow("0", "0")]
    public void TryReadDecimal_WhenJsonIsNumberOrNumericString_ShouldReturnTheValue(string json, string expected)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadDecimal(document.RootElement, out decimal value);

        Assert.IsTrue(success);
        Assert.AreEqual(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    /// <summary>
    /// Verifies that <c>null</c>, a boolean, an object, an array and a non-numeric string are not read as a decimal,
    /// and that reading them does not throw.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("null")]
    [DataRow("true")]
    [DataRow("{ \"value\": 1 }")]
    [DataRow("[ 1 ]")]
    [DataRow("\"invalid\"")]
    public void TryReadDecimal_WhenJsonIsNotNumeric_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadDecimal(document.RootElement, out decimal value);

        Assert.IsFalse(success);
        Assert.AreEqual(0m, value);
    }
}
