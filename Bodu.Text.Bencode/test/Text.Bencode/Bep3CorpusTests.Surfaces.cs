// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bep3CorpusTests.Surfaces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;

namespace Bodu.Text.Bencode;

/// <summary>
/// Extends the conformance-corpus coverage beyond the reader to the other read surfaces: every valid corpus
/// document must also parse through the read-only <see cref="BencodeDocument" />, every valid document whose
/// dictionary keys are valid UTF-8 must survive a mutable <see cref="BencodeNode" /> round-trip that reproduces
/// the input bytes exactly (anything the strict reader accepts is canonical Bencode, and canonical Bencode is a
/// unique encoding), and every malformed corpus document must be rejected by
/// <see cref="BencodeDocument.Parse(byte[])" /> as well.
/// </summary>
public sealed partial class Bep3CorpusTests
{
    /// <summary>
    /// Names the valid corpus rows excluded from the node round-trip: the mutable node DOM stores dictionary keys
    /// as .NET strings, so a key that is not valid UTF-8 is decoded with U+FFFD replacement and re-encodes to
    /// different bytes. The reader and the read-only document surfaces preserve such keys, so these rows remain
    /// covered by every other corpus sweep.
    /// </summary>
    private static readonly HashSet<string> s_nodeRoundTripExclusions = new(StringComparer.Ordinal)
    {
        "unsigned high-byte key ladder",
        "binary key and binary value",
    };

    /// <summary>
    /// Provides the valid corpus rows whose documents contain only UTF-8-representable dictionary keys and can
    /// therefore round-trip through the string-keyed mutable node DOM.
    /// </summary>
    /// <returns>The filtered valid corpus rows.</returns>
    public static IEnumerable<object[]> Bep3CorpusValidNodeRoundTripData() =>
        Bep3CorpusValidData()
            .Where(row => !s_nodeRoundTripExclusions.Contains(((ValidKat<string, BencodeTokenType[]>)row[0]).Name));

    /// <summary>
    /// Verifies that each valid corpus document parses through the read-only <see cref="BencodeDocument" />
    /// surface, with the root element spanning the entire input.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Bep3CorpusValidData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenBep3CorpusDocumentValid_ForDocument_ShouldParse(ValidKat<string, BencodeTokenType[]> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);

        using var document = BencodeDocument.Parse(bytes);

        CollectionAssert.AreEqual(bytes, document.RootElement.GetRawBytes(), kat.Name);
    }

    /// <summary>
    /// Verifies that each valid corpus document with UTF-8-representable dictionary keys round-trips through the
    /// mutable <see cref="BencodeNode" /> tree and back to its exact input bytes.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Bep3CorpusValidNodeRoundTripData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenBep3CorpusDocumentValid_ForNodeRoundTrip_ShouldReproduceExactBytes(ValidKat<string, BencodeTokenType[]> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);

        BencodeNode? node = BencodeNode.Parse(bytes);

        Assert.IsNotNull(node, kat.Name);
        CollectionAssert.AreEqual(bytes, node.ToByteArray(), kat.Name);
    }

    /// <summary>
    /// Verifies that each malformed corpus document is rejected by the read-only <see cref="BencodeDocument" />
    /// surface with <see cref="BencodeFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Bep3CorpusMalformedData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenBep3CorpusDocumentMalformed_ForDocument_ShouldThrowBencodeFormatException(InvalidKat<string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = Bytes(kat.Input);

        var exception = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using var document = BencodeDocument.Parse(bytes);
        });

        Assert.IsInstanceOfType(exception, kat.ExceptionType);
    }
}
