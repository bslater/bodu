// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.TryReadInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class JsonRateValueReaderTests
{
    /// <summary>
    /// Verifies that a JSON integer of any sign, or a JSON string holding one, reads as that integer.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("1672704000", 1672704000L)]
    [DataRow("\"1672704000\"", 1672704000L)]
    [DataRow("-5", -5L)]
    public void TryReadInt64_WhenJsonIsIntegerOrIntegerString_ShouldReturnTheValue(string json, long expected)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadInt64(document.RootElement, out long value);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, value);
    }

    /// <summary>
    /// Verifies that a fractional number, a number beyond the 64-bit range, <c>null</c>, a boolean, an object and a
    /// non-numeric string are not read as an integer, and that reading them does not throw.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1.5")]
    [DataRow("99999999999999999999")]
    [DataRow("null")]
    [DataRow("false")]
    [DataRow("{ }")]
    [DataRow("\"soon\"")]
    public void TryReadInt64_WhenJsonIsNotInteger_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadInt64(document.RootElement, out long value);

        Assert.IsFalse(success);
        Assert.AreEqual(0L, value);
    }
}
