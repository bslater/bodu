// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.ValueSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.ValueSpan" />, verifying the raw bytes it
/// exposes for a field.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that a field holding bytes that are not valid UTF-8 reads without error, that
    /// <see cref="Utf8DelimitedReader.ValueSpan" /> exposes the field's raw bytes, and that the next field reads as usual.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenAFieldHoldsInvalidUtf8_ShouldExposeTheRawBytes()
    {
        byte[] source = [(byte)'x', (byte)'0', (byte)'9', 0x41, 0xB4, 0x1C, (byte)',', .. "aktau"u8];
        var reader = new Utf8DelimitedReader(source, new DelimitedReaderOptions { NoHeader = true });
        var rawValues = new List<byte[]>();
        var values = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType != DelimitedTokenType.String)
                continue;

            rawValues.Add(reader.ValueSpan.ToArray());
            values.Add(reader.GetString());
        }

        Assert.AreEqual(2, rawValues.Count);
        CollectionAssert.AreEqual(new byte[] { 0x78, 0x30, 0x39, 0x41, 0xB4, 0x1C }, rawValues[0]);
        CollectionAssert.AreEqual("aktau"u8.ToArray(), rawValues[1]);
        Assert.AreEqual("aktau", values[1]);
    }
}
