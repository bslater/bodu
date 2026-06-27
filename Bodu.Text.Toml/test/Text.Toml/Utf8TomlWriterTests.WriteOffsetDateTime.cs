// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteOffsetDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteOffsetDateTime" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that the four date-time kinds, including fractional seconds, are emitted in their RFC 3339 canonical
    /// form.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteOffsetDateTime_WhenDateTimeKindsWithFractions_ShouldEmitRfc3339()
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();

            writer.WritePropertyName("odt");
            writer.WriteOffsetDateTime(new DateTimeOffset(2020, 1, 1, 12, 30, 45, 500, TimeSpan.FromHours(2)));

            writer.WritePropertyName("odtz");
            writer.WriteOffsetDateTime(new DateTimeOffset(2020, 1, 1, 12, 30, 45, TimeSpan.Zero));

            writer.WritePropertyName("ldt");
            writer.WriteLocalDateTime(new DateTime(2020, 1, 1, 12, 30, 45).AddTicks(1234567));

            writer.WritePropertyName("ld");
            writer.WriteLocalDate(new DateOnly(2020, 1, 1));

            writer.WritePropertyName("lt");
            writer.WriteLocalTime(new TimeOnly(0, 0, 0).Add(TimeSpan.FromTicks(1000000)));

            writer.WriteEndTable();
        });

        string expected =
            "odt = 2020-01-01T12:30:45.5+02:00\n" +
            "odtz = 2020-01-01T12:30:45Z\n" +
            "ldt = 2020-01-01T12:30:45.1234567\n" +
            "ld = 2020-01-01\n" +
            "lt = 00:00:00.1\n";

        Assert.AreEqual(expected, actual);
    }

}
