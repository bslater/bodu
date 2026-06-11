// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeReader.Read" /> rejects every category of malformed or non-canonical Bencode
/// by throwing <see cref="BencodeFormatException" />, and that canonical edge-case documents are accepted.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that the reader rejects malformed and non-canonical Bencode by throwing
    /// <see cref="BencodeFormatException" /> while it is driven to completion.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The malformed Bencode, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Integer grammar.
    [DataRow("leading zero", "i03e")]
    [DataRow("leading zeros", "i007e")]
    [DataRow("negative zero", "i-0e")]
    [DataRow("negative leading zero", "i-03e")]
    [DataRow("empty integer", "ie")]
    [DataRow("lone minus", "i-e")]
    [DataRow("space in integer", "i e")]
    [DataRow("plus sign", "i+1e")]
    [DataRow("unterminated integer", "i12")]
    [DataRow("integer overflow", "i99999999999999999999e")]
    [DataRow("integer beyond UInt64 by one", "i18446744073709551616e")]
    [DataRow("integer underflow", "i-99999999999999999999e")]
    [DataRow("integer below Int64 by one", "i-9223372036854775809e")]
    [DataRow("non-digit in integer", "i1a2e")]

    // Byte-string grammar.
    [DataRow("missing string separator", "3abc")]
    [DataRow("bad string separator", "3;abc")]
    [DataRow("leading-zero length", "03:abc")]
    [DataRow("length exceeds input", "5:ab")]
    [DataRow("length with no colon at end", "3")]
    [DataRow("only length and colon truncated", "10:short")]
    [DataRow("string length exceeds Int32 by one", "2147483648:x")]
    [DataRow("string length far beyond Int32", "99999999999999999999:x")]

    // Container balance.
    [DataRow("unterminated list", "li1e")]
    [DataRow("unterminated empty list", "l")]
    [DataRow("unterminated dictionary", "d1:ai1e")]
    [DataRow("unterminated empty dictionary", "d")]
    [DataRow("lone end", "e")]
    [DataRow("list closing into nothing", "lee")]
    [DataRow("dictionary value missing", "d1:ae")]

    // Dictionary key rules.
    [DataRow("non-string dictionary key (integer)", "di1ei2ee")]
    [DataRow("non-string dictionary key (list)", "dlei2ee")]
    [DataRow("non-string dictionary key (dict)", "ddei2ee")]
    [DataRow("unordered dictionary keys", "d1:b1:x1:a1:ye")]
    [DataRow("duplicate dictionary keys", "d1:a1:x1:a1:ye")]
    [DataRow("unordered by length", "d2:aa1:x1:a1:ye")]

    // Trailing / leading bytes.
    [DataRow("trailing data after integer", "i1e2:xx")]
    [DataRow("trailing data after list", "le2:xx")]
    [DataRow("trailing data after dictionary", "dei1e")]
    [DataRow("trailing junk byte", "i1ex")]

    // Unknown tokens.
    [DataRow("unknown prefix byte", "x")]
    [DataRow("unknown prefix in list", "lxe")]
    public void Read_WhenInputMalformed_ShouldThrowBencodeFormatException(string testName, string input)
    {
        _ = testName;
        byte[] bytes = Bytes(input);

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            while (reader.Read())
            {
                // Drive the reader to completion to surface any malformed-input error.
            }
        });
    }

    /// <summary>
    /// Verifies that the reader rejects a document nested deeper than the configured maximum depth by throwing
    /// <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestingExceedsMaxDepth_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Bytes("llleee"); // depth 3
        var options = new BencodeReaderOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes, options);
            while (reader.Read())
            {
                // Drive the reader to completion to surface the nesting-too-deep error.
            }
        });
    }

    /// <summary>
    /// Verifies that a document nested exactly to the configured maximum depth is accepted.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestingEqualsMaxDepth_ShouldNotThrow()
    {
        byte[] bytes = Bytes("llee"); // depth 2
        var options = new BencodeReaderOptions { MaxDepth = 2 };

        var reader = new Utf8BencodeReader(bytes, options);
        var count = 0;
        while (reader.Read())
            count++;

        Assert.AreEqual(4, count);
    }

    /// <summary>
    /// Verifies that constructing a reader with a non-positive maximum depth throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> <c>maxDepth</c>.
    /// </summary>
    /// <param name="maxDepth">The invalid maximum depth.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenMaxDepthNotPositive_ShouldThrowArgumentOutOfRangeException(int maxDepth)
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new Utf8BencodeReader([], maxDepth);
        }, "maxDepth");
    }

    /// <summary>
    /// Verifies that the byte offset recorded on the thrown <see cref="BencodeFormatException" /> identifies where
    /// the canonical-form violation was detected.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The malformed Bencode, expressed as Latin-1 text.</param>
    /// <param name="expectedOffset">The expected byte offset on the exception.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("integer at start", "i03e", 0)]
    [DataRow("string at start", "3abc", 0)]
    [DataRow("unordered keys", "d1:b1:x1:a1:ye", 9)]
    [DataRow("duplicate keys", "d1:a1:x1:a1:ye", 9)]
    [DataRow("non-string key", "di1ei2ee", 1)]
    [DataRow("trailing data", "i1e2:xx", 3)]
    [DataRow("unterminated list", "li1e", 4)]
    public void Read_WhenInputMalformed_ShouldRecordOffset(string testName, string input, int expectedOffset)
    {
        _ = testName;
        byte[] bytes = Bytes(input);

        var ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            while (reader.Read())
            {
                // Drive the reader to completion to surface the malformed-input error.
            }
        });

        Assert.AreEqual(expectedOffset, ex.Offset);
    }

    /// <summary>
    /// Verifies that canonical edge-case documents — empty containers and a deeply nested chain within the depth
    /// limit — are accepted without throwing.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="input">The valid Bencode, expressed as Latin-1 text.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("empty list", "le")]
    [DataRow("empty dictionary", "de")]
    [DataRow("nested empty containers", "ldelee")]
    [DataRow("single deeply nested chain", "llllllleeeeeee")]
    [DataRow("integer zero", "i0e")]
    [DataRow("integer min", "i-9223372036854775808e")]
    [DataRow("empty byte string", "0:")]
    [DataRow("ordered keys by length", "d1:a2:aae")]
    public void Read_WhenInputCanonicalEdgeCase_ShouldNotThrow(string testName, string input)
    {
        _ = testName;
        byte[] bytes = Bytes(input);

        var reader = new Utf8BencodeReader(bytes);
        while (reader.Read())
        {
            // Drive the reader to completion; a valid document must not raise.
        }

        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
    }
}
