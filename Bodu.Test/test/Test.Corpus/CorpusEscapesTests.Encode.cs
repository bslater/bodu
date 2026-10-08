// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusEscapesTests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class CorpusEscapesTests
{
    /// <summary>
    /// Verifies that encoding writes printable ASCII as itself, the short escapes for the named controls and a
    /// backslash, and <c>\xHH</c> for every other byte.
    /// </summary>
    [TestMethod]
    public void Encode_WhenBytesMixPrintableAndControl_ShouldUseTheShortestEscapes()
    {
        byte[] bytes = [(byte)'a', (byte)'\\', 0x0A, 0x0D, 0x09, 0x00, 0x7F, 0xC3, 0xA9];

        string encoded = CorpusEscapes.Encode(bytes);

        Assert.AreEqual(@"a\\\n\r\t\0\x7F\xC3\xA9", encoded);
    }

    /// <summary>
    /// Verifies that every byte value survives an encode and decode round trip.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEveryByteValueIsEncoded_ShouldDecodeBackToTheSameBytes()
    {
        byte[] bytes = [.. Enumerable.Range(0, 256).Select(value => (byte)value)];

        byte[] decoded = CorpusEscapes.Decode(CorpusEscapes.Encode(bytes));

        CollectionAssert.AreEqual(bytes, decoded);
    }
}
