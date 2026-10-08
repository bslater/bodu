// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.ValueSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="Utf8BencodeReader.ValueSpan" />: that it describes the current token only, whatever token came
/// before it.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueSpan" /> is empty before the first token is read.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenNoTokenHasBeenRead_ShouldBeEmpty()
    {
        var reader = new Utf8BencodeReader(Bytes("4:spam"));

        Assert.AreEqual(0, reader.ValueSpan.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueSpan" /> on an integer token holds the integer's text without its
    /// <c>i</c> and <c>e</c> delimiters, even when a byte string was read just before it.
    /// </summary>
    /// <param name="encoded">A list holding a byte string followed by the integer.</param>
    /// <param name="expected">The integer's text.</param>
    [TestMethod]
    [DataRow("l4:spami42ee", "42")]
    [DataRow("l4:spami-7ee", "-7")]
    [DataRow("l4:spami0ee", "0")]
    [DataRow("l4:spami18446744073709551615ee", "18446744073709551615")]
    public void ValueSpan_WhenTokenIsInteger_ShouldHoldItsDigits(string encoded, string expected)
    {
        var reader = new Utf8BencodeReader(Bytes(encoded));
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);

        string actual = Encoding.ASCII.GetString(reader.ValueSpan);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueSpan" /> is empty on every list and dictionary start and end
    /// token, including those that follow a byte string or a key.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenTokenIsContainerBoundary_ShouldBeEmpty()
    {
        var reader = new Utf8BencodeReader(Bytes("l4:spamd1:a4:eggseli7eee"));
        var boundaries = new List<(BencodeTokenType TokenType, int Length)>();

        while (reader.Read())
        {
            if (reader.TokenType is BencodeTokenType.StartList or BencodeTokenType.EndList or BencodeTokenType.StartDictionary or BencodeTokenType.EndDictionary)
                boundaries.Add((reader.TokenType, reader.ValueSpan.Length));
        }

        CollectionAssert.AreEqual(
            new (BencodeTokenType, int)[]
            {
                (BencodeTokenType.StartList, 0),
                (BencodeTokenType.StartDictionary, 0),
                (BencodeTokenType.EndDictionary, 0),
                (BencodeTokenType.StartList, 0),
                (BencodeTokenType.EndList, 0),
                (BencodeTokenType.EndList, 0),
            },
            boundaries);
    }
}
