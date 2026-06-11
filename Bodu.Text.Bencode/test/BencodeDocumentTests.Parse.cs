// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="BencodeDocument.Parse(byte[])" /> and its overloads accept exactly the canonical Bencode
/// grammar — rejecting malformed input, trailing bytes, and over-deep nesting with
/// <see cref="BencodeFormatException" /> — and that the parsed document is independent of the source buffer.
/// </summary>
public partial class BencodeDocumentTests
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
    [DataRow("lone minus", "i-e")]
    [DataRow("plus sign", "i+1e")]
    [DataRow("unterminated integer", "i12")]
    [DataRow("integer overflow", "i99999999999999999999e")]
    [DataRow("non-digit in integer", "i1a2e")]

    // Byte-string grammar.
    [DataRow("missing string separator", "3abc")]
    [DataRow("leading-zero length", "03:abc")]
    [DataRow("length exceeds input", "5:ab")]
    [DataRow("length with no colon at end", "3")]

    // Container balance.
    [DataRow("unterminated list", "li1e")]
    [DataRow("unterminated dictionary", "d1:ai1e")]
    [DataRow("lone end", "e")]
    [DataRow("list closing into nothing", "lee")]
    [DataRow("dictionary value missing", "d1:ae")]

    // Dictionary key rules.
    [DataRow("non-string dictionary key", "di1ei2ee")]
    [DataRow("unordered dictionary keys", "d1:b1:x1:a1:ye")]
    [DataRow("duplicate dictionary keys", "d1:a1:x1:a1:ye")]

    // Trailing / leading bytes.
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
        byte[] bytes = Bytes(input);

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using BencodeDocument document = BencodeDocument.Parse(bytes);
        });
    }

    /// <summary>
    /// Verifies that canonical edge-case documents — empty containers, integer extremes, and an empty byte string —
    /// parse into a document whose root reports the expected kind.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The valid Bencode, expressed as Latin-1 text.</param>
    /// <param name="expectedKind">The expected root value kind.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("empty list", "le", BencodeValueKind.Array)]
    [DataRow("empty dictionary", "de", BencodeValueKind.Object)]
    [DataRow("nested empty containers", "ldelee", BencodeValueKind.Array)]
    [DataRow("integer zero", "i0e", BencodeValueKind.Integer)]
    [DataRow("integer min", "i-9223372036854775808e", BencodeValueKind.Integer)]
    [DataRow("integer max", "i9223372036854775807e", BencodeValueKind.Integer)]
    [DataRow("empty byte string", "0:", BencodeValueKind.ByteString)]
    [DataRow("ordered keys by length", "d1:a2:aae", BencodeValueKind.Object)]
    public void Parse_WhenInputCanonicalEdgeCase_ShouldExposeRoot(string testName, string input, BencodeValueKind expectedKind)
    {
        _ = testName;
        using BencodeDocument document = BencodeDocument.Parse(Bytes(input));

        Assert.AreEqual(expectedKind, document.RootElement.ValueKind);
    }

    /// <summary>
    /// Verifies that parsing input with trailing bytes after a complete root value throws
    /// <see cref="BencodeFormatException" />, because a document must be a single value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTrailingBytesAfterRoot_ShouldThrowBencodeFormatException()
    {
        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using BencodeDocument document = BencodeDocument.Parse(Bytes("i1e2:xx"));
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
            using BencodeDocument document = BencodeDocument.Parse([]);
        });
    }

    /// <summary>
    /// Verifies that a document nested exactly to the configured <see cref="BencodeDocumentOptions.MaxDepth" /> is
    /// accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNestingEqualsMaxDepth_ShouldSucceed()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("llee"), new BencodeDocumentOptions { MaxDepth = 2 });

        Assert.AreEqual(BencodeValueKind.Array, document.RootElement.ValueKind);
        Assert.AreEqual(1, document.RootElement.GetArrayLength());
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeDocumentOptions.MaxDepth" /> of zero or less selects the default maximum
    /// depth instead of rejecting all nesting.
    /// </summary>
    /// <param name="maxDepth">The non-positive depth that selects the default.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Parse_WhenMaxDepthNotPositive_ShouldUseDefaultDepth(int maxDepth)
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("llleee"), new BencodeDocumentOptions { MaxDepth = maxDepth });

        Assert.AreEqual(BencodeValueKind.Array, document.RootElement.ValueKind);
    }

    /// <summary>
    /// Verifies that a document nested deeper than the default maximum depth of 256 throws
    /// <see cref="BencodeFormatException" />, and that nesting exactly to the default is accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestingExceedsDefaultMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] atLimit = Bytes(new string('l', 256) + new string('e', 256));
        byte[] beyondLimit = Bytes(new string('l', 257) + new string('e', 257));

        using BencodeDocument document = BencodeDocument.Parse(atLimit);
        Assert.AreEqual(BencodeValueKind.Array, document.RootElement.ValueKind);

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using BencodeDocument deep = BencodeDocument.Parse(beyondLimit);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeDocument.Parse(byte[], BencodeDocumentOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArrayIsNull_ForOptionsOverload_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodeDocument.Parse(null!, new BencodeDocumentOptions { MaxDepth = 4 });
        });
    }

    /// <summary>
    /// Verifies that the document parses a private copy of the source, so mutating the source array after parsing
    /// does not change the values the document exposes.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSourceMutatedAfterParse_ShouldNotAffectDocument()
    {
        byte[] source = Bytes("4:spam");
        using BencodeDocument document = BencodeDocument.Parse(source);

        source[2] = (byte)'X';

        Assert.AreEqual("spam", document.RootElement.GetString());
    }
}
