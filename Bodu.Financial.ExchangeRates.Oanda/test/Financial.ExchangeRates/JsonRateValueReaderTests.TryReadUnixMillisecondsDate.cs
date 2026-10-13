// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReaderTests.TryReadUnixMillisecondsDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class JsonRateValueReaderTests
{
    /// <summary>
    /// Verifies that a JSON integer, or a JSON string holding one, reads as the UTC date of that Unix-millisecond
    /// timestamp.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1700000000000")]
    [DataRow("\"1700000000000\"")]
    public void TryReadUnixMillisecondsDate_WhenJsonIsIntegerOrIntegerString_ShouldReturnItsUtcDate(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadUnixMillisecondsDate(document.RootElement, out DateOnly date);

        Assert.IsTrue(success);
        Assert.AreEqual(new DateOnly(2023, 11, 14), date);
    }

    /// <summary>
    /// Verifies that the first and last milliseconds of a UTC day both read as that day.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1672704000000")]
    [DataRow("1672790399999")]
    public void TryReadUnixMillisecondsDate_WhenTimestampIsWithinADay_ShouldReturnThatUtcDay(string json)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadUnixMillisecondsDate(document.RootElement, out DateOnly date);

        Assert.IsTrue(success);
        Assert.AreEqual(new DateOnly(2023, 1, 3), date);
    }

    /// <summary>
    /// Verifies that the earliest and latest timestamps <see cref="DateTimeOffset" /> represents read as the first and
    /// last dates of its range.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    /// <param name="expected">The expected date, as <c>yyyy-MM-dd</c>.</param>
    [TestMethod]
    [DataRow("-62135596800000", "0001-01-01")]
    [DataRow("253402300799999", "9999-12-31")]
    public void TryReadUnixMillisecondsDate_WhenTimestampIsAtAnEndOfTheRange_ShouldReturnThatDate(string json, string expected)
    {
        using var document = JsonDocument.Parse(json);

        bool success = JsonRateValueReader.TryReadUnixMillisecondsDate(document.RootElement, out DateOnly date);

        Assert.IsTrue(success);
        Assert.AreEqual(DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture), date);
    }

    /// <summary>
    /// Verifies that a timestamp outside the range <see cref="DateTimeOffset" /> represents is not read as a date, and
    /// does not throw.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("-62135596800001")]
    [DataRow("253402300800000")]
    [DataRow("9223372036854775807")]
    [DataRow("-9223372036854775808")]
    public void TryReadUnixMillisecondsDate_WhenTimestampIsOutsideTheRange_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.IsFalse(JsonRateValueReader.TryReadUnixMillisecondsDate(document.RootElement, out _));
    }

    /// <summary>
    /// Verifies that a value that is not an integer timestamp is not read as a date.
    /// </summary>
    /// <param name="json">The JSON value to read.</param>
    [TestMethod]
    [DataRow("1672704000000.5")]
    [DataRow("\"soon\"")]
    [DataRow("null")]
    [DataRow("true")]
    [DataRow("{}")]
    [DataRow("[]")]
    public void TryReadUnixMillisecondsDate_WhenJsonIsNotAnIntegerTimestamp_ShouldReturnFalse(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.IsFalse(JsonRateValueReader.TryReadUnixMillisecondsDate(document.RootElement, out _));
    }
}
