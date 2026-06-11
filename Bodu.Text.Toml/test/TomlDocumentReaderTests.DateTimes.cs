// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.DateTimes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that an offset date-time with a positive offset decodes to the expected instant.
    /// </summary>
    [TestMethod]
    public void Read_WhenOffsetDateTimePositiveOffset_ShouldDecodeInstant()
    {
        TomlDocumentReader reader = Create("v = 1979-05-27T07:32:00+02:00\n");

        ExpectSingleValue(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.FromHours(2)), reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that an explicit <c>+00:00</c> offset decodes to the same zero-offset instant as the <c>Z</c> form.
    /// </summary>
    [TestMethod]
    public void Read_WhenOffsetDateTimePlusZeroOffset_ShouldDecodeAsZeroOffset()
    {
        TomlDocumentReader reader = Create("v = 1979-05-27T07:32:00+00:00\n");

        ExpectSingleValue(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a lowercase <c>t</c> date/time separator and a lowercase <c>z</c> offset are accepted.
    /// </summary>
    [TestMethod]
    public void Read_WhenOffsetDateTimeUsesLowercaseSeparators_ShouldDecodeInstant()
    {
        TomlDocumentReader reader = Create("v = 1979-05-27t07:32:00z\n");

        ExpectSingleValue(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a space-separated date and time (the RFC 3339 alternative to <c>T</c>) decodes to a local
    /// date-time.
    /// </summary>
    [TestMethod]
    public void Read_WhenLocalDateTimeSpaceSeparated_ShouldDecodeDateTime()
    {
        TomlDocumentReader reader = Create("v = 1979-05-27 07:32:00\n");

        ExpectSingleValue(ref reader, TomlTokenType.LocalDateTime);
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified), reader.GetDateTime());
    }

    /// <summary>
    /// Verifies that fractional seconds of varying precision decode into the time component, truncating beyond
    /// 100-nanosecond resolution.
    /// </summary>
    /// <param name="fraction">The fractional-second digits, excluding the decimal point.</param>
    /// <param name="expectedTicks">The expected sub-second tick count.</param>
    [TestMethod]
    [DataRow("5", 5_000_000L, DisplayName = "tenths")]
    [DataRow("123", 1_230_000L, DisplayName = "milliseconds")]
    [DataRow("1234567", 1_234_567L, DisplayName = "full tick precision")]
    [DataRow("123456789", 1_234_567L, DisplayName = "truncated beyond ticks")]
    public void Read_WhenLocalTimeHasFractionalSeconds_ShouldDecodeTruncatedToTicks(string fraction, long expectedTicks)
    {
        TomlDocumentReader reader = Create($"v = 07:32:10.{fraction}\n");

        ExpectSingleValue(ref reader, TomlTokenType.LocalTime);

        TimeOnly expected = new TimeOnly(7, 32, 10).Add(TimeSpan.FromTicks(expectedTicks));
        Assert.AreEqual(expected, reader.GetTimeOnly());
    }

    /// <summary>
    /// Verifies that a bare local time without a date decodes to the expected <see cref="TimeOnly" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLocalTimeMidnight_ShouldDecodeTimeOnly()
    {
        TomlDocumentReader reader = Create("v = 00:00:00\n");

        ExpectSingleValue(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(new TimeOnly(0, 0, 0), reader.GetTimeOnly());
    }

    /// <summary>
    /// Verifies that out-of-range date and time components are rejected with <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="literal">The malformed date-time literal under test.</param>
    [TestMethod]
    [DataRow("2020-13-01", DisplayName = "month 13")]
    [DataRow("2020-00-01", DisplayName = "month 0")]
    [DataRow("2020-01-32", DisplayName = "day 32")]
    [DataRow("2020-02-30", DisplayName = "impossible February day")]
    [DataRow("25:00:00", DisplayName = "hour 25")]
    [DataRow("12:60:00", DisplayName = "minute 60")]
    [DataRow("12:00:61", DisplayName = "second 61")]
    [DataRow("2020-01-01T07:32", DisplayName = "datetime without seconds")]
    [TestCategory("Regression")]
    public void Read_WhenDateTimeComponentOutOfRange_ShouldThrowTomlFormatException(string literal)
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create($"v = {literal}\n");
        });
    }

    /// <summary>
    /// Verifies that a leap-second time value is rejected with <see cref="TomlFormatException" /> because the .NET time
    /// types cannot represent it.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeIsLeapSecond_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("v = 23:59:60\n");
        });
    }
}
