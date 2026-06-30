// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteInteger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteInteger" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that integer values, including the signed 64-bit boundaries and zero, are emitted in decimal form.
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    /// <param name="expected">The expected emitted value text.</param>
    [TestMethod]
    [DataRow(0L, "0", DisplayName = "zero")]
    [DataRow(42L, "42", DisplayName = "positive")]
    [DataRow(-42L, "-42", DisplayName = "negative")]
    [DataRow(long.MaxValue, "9223372036854775807", DisplayName = "Int64 max")]
    [DataRow(long.MinValue, "-9223372036854775808", DisplayName = "Int64 min")]
    public void WriteInteger_WhenValue_ShouldEmitDecimal(long value, string expected)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteInteger(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

    /// <summary>
    /// Verifies that writing a value into a table without first supplying a property name throws
    /// <see cref="InvalidOperationException" /> at the offending call, rather than deferring failure to emit time.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenWrittenWithoutPropertyName_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WriteInteger(1);
            writer.WriteEndTable();
        });
    }

    /// <summary>
    /// Verifies that the paired overloads enforce the same state rules as the separate calls — a duplicate key is
    /// rejected.
    /// </summary>
    [TestMethod]
    public void WriteInteger_WhenNamedPairRepeatsKey_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8TomlWriter(buffer);
            writer.WriteStartTable();
            writer.WriteInteger("a", 1);
            writer.WriteInteger("a", 2);
        });
    }

}
