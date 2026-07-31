// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bep3CorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Contains the BEP-3 conformance corpus — a comprehensive sweep of valid and malformed Bencode documents adapted
/// from the normative grammar and literal examples of BEP 3 (<c>bittorrent.org/beps/bep_0003.html</c>), the KRPC
/// message shapes of BEP 5, and the malformed-input and torture cases exercised by mainstream battle-tested
/// implementations — libtorrent's <c>test_bdecode.cpp</c>, Transmission's benc/variant tests,
/// <c>bencodepy</c>/<c>bencode.py</c>, <c>bencode-go</c>, and Rust's <c>bendy</c>/<c>serde_bencode</c> suites —
/// re-expressed as KAT rows and run in the Regression tier.
/// </summary>
[TestClass]
public sealed partial class Bep3CorpusTests
{
    /// <summary>
    /// Decodes the supplied Latin-1 text to bytes so binary content in the 0x00–0xFF range survives unchanged.
    /// </summary>
    /// <param name="text">The Latin-1 text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Bytes(string text) =>
        Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// Verifies that each valid corpus document produces exactly its expected token sequence, consumes every input
    /// byte, and leaves the reader at <see cref="BencodeTokenType.None" />.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Bep3CorpusValidData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenBep3CorpusDocumentValid_ShouldProduceExpectedTokenSequence(ValidKat<string, BencodeTokenType[]> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);
        var reader = new Utf8BencodeReader(bytes);

        var actual = new List<BencodeTokenType>();
        while (reader.Read())
            actual.Add(reader.TokenType);

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Name);
        Assert.AreEqual(bytes.Length, reader.BytesConsumed, "bytes consumed");
        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
    }

    /// <summary>
    /// Verifies that each malformed corpus document throws <see cref="BencodeFormatException" /> while the reader
    /// is driven to completion.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Bep3CorpusMalformedData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenBep3CorpusDocumentMalformed_ShouldThrowBencodeFormatException(InvalidKat<string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);

        var exception = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var reader = new Utf8BencodeReader(bytes);
            while (reader.Read())
            {
                // Drive the reader to completion to surface the malformed-input error.
            }
        });

        Assert.IsInstanceOfType(exception, kat.ExceptionType);
    }
}
