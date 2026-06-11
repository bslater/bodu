// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.TokenStartIndex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="Utf8BencodeReader.TokenStartIndex" />: that every token kind reports the offset of its first
/// byte, including the length prefix of byte strings and property names.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TokenStartIndex" /> reports zero before the first read and the
    /// first byte of every token thereafter, walking a document that exercises all token kinds.
    /// </summary>
    [TestMethod]
    public void TokenStartIndex_WhenWalkingDocument_ShouldReportEachTokenStart()
    {
        // Layout: d(0) 3:abc(1..5) l(6) i42e(7..10) e(11) e(12)
        byte[] bytes = Bytes("d" + "3:abc" + "li42e" + "e" + "e");
        var reader = new Utf8BencodeReader(bytes);

        Assert.AreEqual(0, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // StartDictionary at 0
        Assert.AreEqual(0, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // PropertyName "abc": token starts at the length prefix
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual(1, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // StartList at 6
        Assert.AreEqual(6, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // Integer i42e at 7
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(7, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // EndList at 11
        Assert.AreEqual(11, reader.TokenStartIndex);

        Assert.IsTrue(reader.Read()); // EndDictionary at 12
        Assert.AreEqual(12, reader.TokenStartIndex);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TokenStartIndex" /> combined with
    /// <see cref="Utf8BencodeReader.BytesConsumed" /> brackets a scalar token's complete encoded form, the raw-slice
    /// recovery pattern at the reader level.
    /// </summary>
    [TestMethod]
    public void TokenStartIndex_WhenOnScalarToken_ShouldBracketRawTokenWithBytesConsumed()
    {
        byte[] bytes = Bytes("li42e4:spame");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList

        Assert.IsTrue(reader.Read()); // i42e
        Assert.AreEqual("i42e", System.Text.Encoding.Latin1.GetString(bytes[reader.TokenStartIndex..reader.BytesConsumed]));

        Assert.IsTrue(reader.Read()); // 4:spam
        Assert.AreEqual("4:spam", System.Text.Encoding.Latin1.GetString(bytes[reader.TokenStartIndex..reader.BytesConsumed]));
    }
}
