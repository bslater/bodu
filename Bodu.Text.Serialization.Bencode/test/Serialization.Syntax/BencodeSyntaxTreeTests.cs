// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSyntaxTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Serialization.Bencode.Syntax;

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Verifies that the Bencode parser accepts canonical documents losslessly and rejects malformed input.
/// </summary>
[TestClass]
public class BencodeSyntaxTreeTests
{
    /// <summary>
    /// Verifies that parsing a valid document and re-encoding it reproduces the original bytes exactly.
    /// </summary>
    /// <param name="input">The valid Bencode document, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("i42e")]
    [DataRow("i0e")]
    [DataRow("i-7e")]
    [DataRow("4:spam")]
    [DataRow("0:")]
    [DataRow("le")]
    [DataRow("li1ei2ei3ee")]
    [DataRow("de")]
    [DataRow("d3:cow3:moo4:spam4:eggse")]
    [DataRow("d4:spaml1:a1:bee")]
    [DataRow("d8:announce4:test4:infod6:lengthi512e4:name4:abcdee")]
    public void Parse_WhenDocumentIsValid_ShouldRoundTripLosslessly(string input)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(input);

        BencodeDocumentSyntax document = BencodeSyntaxTree.Parse(bytes);

        CollectionAssert.AreEqual(bytes, document.ToByteArray());
    }

    /// <summary>
    /// Verifies that parsing a malformed document throws <see cref="BencodeFormatException" />.
    /// </summary>
    /// <param name="name">A label describing the malformed scenario.</param>
    /// <param name="input">The malformed Bencode document, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("leading zeros", "i03e")]
    [DataRow("negative zero", "i-0e")]
    [DataRow("empty integer", "ie")]
    [DataRow("unterminated integer", "i12")]
    [DataRow("length leading zero", "01:a")]
    [DataRow("string too short", "5:abc")]
    [DataRow("missing separator", "3abc")]
    [DataRow("unsorted keys", "d1:b1:v1:a1:we")]
    [DataRow("duplicate key", "d1:a1:x1:a1:ye")]
    [DataRow("non-string key", "di1e1:ve")]
    [DataRow("unterminated dictionary", "d3:fooi1e")]
    [DataRow("unterminated list", "li1e")]
    [DataRow("trailing data", "i1ei2e")]
    [DataRow("expected value", "x")]
    public void Parse_WhenDocumentIsMalformed_ShouldThrowBencodeFormatException(string name, string input)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(input);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSyntaxTree.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that nesting beyond the configured maximum depth throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("lllllee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSyntaxTree.Parse(bytes, maxDepth: 2);
        });
    }
}
