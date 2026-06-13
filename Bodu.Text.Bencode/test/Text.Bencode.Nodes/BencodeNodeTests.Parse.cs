// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies that <see cref="BencodeNode.Parse(ReadOnlySpan{byte})" /> materializes the correct node kinds from
/// canonical Bencode, rejects malformed input and trailing bytes with <see cref="BencodeFormatException" />, and
/// round-trips byte-for-byte through <see cref="BencodeNode.ToByteArray" />.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that parsing malformed or non-canonical Bencode throws <see cref="BencodeFormatException" />.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The malformed Bencode, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Integer grammar.
    [DataRow("leading zero", "i03e")]
    [DataRow("negative zero", "i-0e")]
    [DataRow("empty integer", "ie")]
    [DataRow("plus sign", "i+1e")]
    [DataRow("unterminated integer", "i12")]
    [DataRow("integer overflow", "i99999999999999999999e")]

    // Byte-string grammar.
    [DataRow("missing string separator", "3abc")]
    [DataRow("leading-zero length", "03:abc")]
    [DataRow("length exceeds input", "5:ab")]

    // Container balance.
    [DataRow("unterminated list", "li1e")]
    [DataRow("unterminated dictionary", "d1:ai1e")]
    [DataRow("lone end", "e")]
    [DataRow("dictionary value missing", "d1:ae")]

    // Dictionary key rules.
    [DataRow("non-string dictionary key", "di1ei2ee")]
    [DataRow("unordered dictionary keys", "d1:b1:x1:a1:ye")]
    [DataRow("duplicate dictionary keys", "d1:a1:x1:a1:ye")]

    // Trailing bytes.
    [DataRow("trailing data after integer", "i1e2:xx")]
    [DataRow("trailing data after list", "le2:xx")]
    [DataRow("trailing data after dictionary", "dei1e")]
    [DataRow("trailing junk byte", "i1ex")]

    // Unknown tokens.
    [DataRow("unknown prefix byte", "x")]
    [DataRow("unknown prefix in list", "lxe")]
    public void Parse_WhenInputMalformed_ShouldThrowBencodeFormatException(string testName, string input)
    {
        _ = testName;
        var bytes = Bytes(input);

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeNode.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that parsing empty input throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputEmpty_ShouldThrowBencodeFormatException()
    {
        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeNode.Parse(ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that parsing input with trailing bytes after a complete root value throws
    /// <see cref="BencodeFormatException" />, because a document must be a single value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTrailingBytesAfterRoot_ShouldThrowBencodeFormatException()
    {
        var bytes = Bytes("i1e2:xx");

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeNode.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that parsing an integer document yields an integer-kind <see cref="BencodeValue" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsInteger_ShouldReturnIntegerValue()
    {
        var node = BencodeNode.Parse(Bytes("i-42e"));

        Assert.IsInstanceOfType<BencodeValue>(node);
        Assert.AreEqual(BencodeValueKind.Integer, node!.GetValueKind());
        Assert.AreEqual(-42L, node.GetValue<long>());
    }

    /// <summary>
    /// Verifies that parsing a byte-string document yields a byte-string-kind <see cref="BencodeValue" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsByteString_ShouldReturnByteStringValue()
    {
        var node = BencodeNode.Parse(Bytes("4:spam"));

        Assert.IsInstanceOfType<BencodeValue>(node);
        Assert.AreEqual(BencodeValueKind.ByteString, node!.GetValueKind());
        Assert.AreEqual("spam", node.GetValue<string>());
    }

    /// <summary>
    /// Verifies that parsing a list document yields a <see cref="BencodeArray" /> with the elements in document
    /// order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsList_ShouldReturnArrayWithElementsInOrder()
    {
        var node = BencodeNode.Parse(Bytes("li1e3:twoli3eee"));

        BencodeArray array = node!.AsArray();
        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(1L, (long)array[0]!);
        Assert.AreEqual("two", (string)array[1]!);
        Assert.AreEqual(3L, (long)array[2]![0]!);
    }

    /// <summary>
    /// Verifies that parsing canonical documents and re-serializing the resulting trees reproduces the source
    /// byte-for-byte.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The canonical Bencode, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("empty list", "le")]
    [DataRow("empty dictionary", "de")]
    [DataRow("empty byte string", "0:")]
    [DataRow("integer extremes", "li-9223372036854775808ei9223372036854775807ee")]
    [DataRow("nested containers", "d1:ali1ed1:bi2eee1:c3:xyze")]
    [DataRow("torrent shape", "d8:announce17:http://tracker.tx4:infod6:lengthi1024e4:name4:testee")]
    public void Parse_WhenInputCanonical_ShouldRoundTripThroughToByteArray(string testName, string input)
    {
        _ = testName;
        var bytes = Bytes(input);

        var roundTripped = BencodeNode.Parse(bytes)!.ToByteArray();

        CollectionAssert.AreEqual(bytes, roundTripped);
    }

    /// <summary>
    /// Verifies that parsing a nested document links every child node to its container, so
    /// <see cref="BencodeNode.Root" /> walks back to the parsed root.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputNested_ShouldBuildParentLinks()
    {
        var root = BencodeNode.Parse(Bytes("d4:listli1eee"));

        BencodeNode list = root!.AsObject()["list"]!;
        BencodeNode leaf = list.AsArray()[0]!;

        Assert.AreSame(root, list.Parent);
        Assert.AreSame(list, leaf.Parent);
        Assert.AreSame(root, leaf.Root);
        Assert.IsNull(root.Parent);
    }
}
