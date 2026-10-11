// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.TryReadUnixMilliseconds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class JsonRateValueReaderTests
{
    /// <summary>
    /// Verifies that a JSON integer, or a JSON string holding one, reads as that Unix-millisecond timestamp.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1700000000000")]
    [DataRow("\"1700000000000\"")]
    public void TryReadUnixMilliseconds_WhenJsonIsIntegerOrIntegerString_ShouldReturnTheValue(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadUnixMilliseconds(document.RootElement, out long timestamp);

        Assert.IsTrue(success);
        Assert.AreEqual(1700000000000L, timestamp);
    }

    /// <summary>
    /// Verifies that a fractional number and <c>null</c> are not read as a timestamp.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1.2")]
    [DataRow("null")]
    public void TryReadUnixMilliseconds_WhenJsonIsNotInteger_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.IsFalse(JsonRateValueReader.TryReadUnixMilliseconds(document.RootElement, out _));
    }
}
