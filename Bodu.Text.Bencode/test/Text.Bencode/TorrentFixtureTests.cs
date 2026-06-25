// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TorrentFixtureTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that an authentic single-file <c>.torrent</c> fixture — with real SHA-1 piece digests that are not valid
/// UTF-8 — parses identically through all three read surfaces (the token reader, the read-only document, and the
/// serializer's POCO binding), pinning lossless binary <c>pieces</c> handling end to end and the
/// info-hash-from-raw-bytes workflow.
/// </summary>
[TestClass]
public class TorrentFixtureTests
{
    /// <summary>
    /// The SHA-1 hash of the fixture's <c>info</c> dictionary slice, computed independently of this library when the
    /// fixture was generated.
    /// </summary>
    private const string ExpectedInfoHash = "f98cd9393539251cfeea8745e7a56031c84236ee";

    /// <summary>
    /// Loads the embedded <c>sample.torrent</c> fixture bytes.
    /// </summary>
    /// <returns>The fixture bytes.</returns>
    private static byte[] LoadFixture()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bodu.Fixtures.sample.torrent")
            ?? throw new InvalidOperationException("The sample.torrent fixture is not embedded.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    /// Verifies that the token reader walks the entire torrent, recovers the binary <c>pieces</c> losslessly, and
    /// consumes every byte.
    /// </summary>
    [TestMethod]
    public void Read_WhenTorrentFixture_ShouldRecoverBinaryPiecesLosslessly()
    {
        byte[] data = LoadFixture();
        byte[]? pieces = null;

        var reader = new Utf8BencodeReader(data);
        while (reader.Read())
        {
            if (reader.TokenType == BencodeTokenType.PropertyName && reader.ValueTextEquals("pieces"u8))
            {
                _ = reader.Read();
                pieces = reader.GetBytes();
            }
        }

        Assert.AreEqual(data.Length, reader.BytesConsumed);
        Assert.IsNotNull(pieces);
        Assert.HasCount(40, pieces); // two SHA-1 digests
    }

    /// <summary>
    /// Verifies that the document surface reproduces the torrent's scalar fields and that hashing the raw bytes of
    /// the <c>info</c> element yields the fixture's known info-hash — the workflow
    /// <see cref="BencodeElement.GetRawBytes" /> exists for.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTorrentFixture_ShouldComputeKnownInfoHashFromRawBytes()
    {
        byte[] data = LoadFixture();
        using var document = BencodeDocument.Parse(data);
        BencodeElement root = document.RootElement;

        Assert.AreEqual("http://tracker.example.com:6969/announce", root.GetProperty("announce").GetString());
        Assert.AreEqual(1749600000L, root.GetProperty("creation date").GetInt64());

        BencodeElement info = root.GetProperty("info");
        Assert.AreEqual("sample.bin", info.GetProperty("name").GetString());
        Assert.AreEqual(16384L, info.GetProperty("piece length").GetInt64());
        Assert.AreEqual(32768L, info.GetProperty("length").GetInt64());
        Assert.HasCount(40, info.GetProperty("pieces").GetBytes());

        string infoHash = Convert.ToHexString(SHA1.HashData(info.GetRawBytes())).ToLowerInvariant();
        Assert.AreEqual(ExpectedInfoHash, infoHash);
    }

    /// <summary>
    /// Verifies that the node tree exposes the torrent and re-serializes it byte for byte, the canonical-form
    /// round-trip guarantee.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTorrentFixtureAsNodeTree_ShouldRoundTripExactBytes()
    {
        byte[] data = LoadFixture();

        var node = BencodeNode.Parse(data);

        Assert.IsNotNull(node);
        Assert.AreEqual("sample.bin", node["info"]!["name"]!.GetValue<string>());
        CollectionAssert.AreEqual(data, node.ToByteArray());
    }

    /// <summary>
    /// Verifies that the serializer binds the torrent to a POCO model — including the binary <c>pieces</c> field as
    /// a byte array — and re-serializes it byte for byte.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTorrentFixture_ShouldBindPocoAndRoundTripExactBytes()
    {
        byte[] data = LoadFixture();

        TorrentModel torrent = BencodeSerializer.Deserialize<TorrentModel>(data);

        Assert.AreEqual("http://tracker.example.com:6969/announce", torrent.Announce);
        Assert.AreEqual("Bodu.Text.Bencode test fixture", torrent.Comment);
        Assert.AreEqual(1749600000L, torrent.CreationDate);
        Assert.IsNotNull(torrent.Info);
        Assert.AreEqual("sample.bin", torrent.Info.Name);
        Assert.AreEqual(16384L, torrent.Info.PieceLength);
        Assert.AreEqual(32768L, torrent.Info.Length);
        Assert.HasCount(40, torrent.Info.Pieces);

        CollectionAssert.AreEqual(data, BencodeSerializer.Serialize(torrent));
    }

    /// <summary>
    /// A POCO model of a single-file torrent's top-level dictionary.
    /// </summary>
    private sealed class TorrentModel
    {
        /// <summary>Gets or sets the tracker announce URL.</summary>
        /// <value>The announce URL.</value>
        [BencodePropertyName("announce")]
        public string Announce { get; set; } = string.Empty;

        /// <summary>Gets or sets the free-form comment.</summary>
        /// <value>The comment.</value>
        [BencodePropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Gets or sets the creator label.</summary>
        /// <value>The creator label.</value>
        [BencodePropertyName("created by")]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>Gets or sets the creation time as a Unix timestamp.</summary>
        /// <value>The creation timestamp.</value>
        [BencodePropertyName("creation date")]
        public long CreationDate { get; set; }

        /// <summary>Gets or sets the info dictionary.</summary>
        /// <value>The info dictionary.</value>
        [BencodePropertyName("info")]
        public TorrentInfoModel? Info { get; set; }
    }

    /// <summary>
    /// A POCO model of a single-file torrent's <c>info</c> dictionary.
    /// </summary>
    private sealed class TorrentInfoModel
    {
        /// <summary>Gets or sets the total content length in bytes.</summary>
        /// <value>The content length.</value>
        [BencodePropertyName("length")]
        public long Length { get; set; }

        /// <summary>Gets or sets the suggested file name.</summary>
        /// <value>The file name.</value>
        [BencodePropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the piece size in bytes.</summary>
        /// <value>The piece size.</value>
        [BencodePropertyName("piece length")]
        public long PieceLength { get; set; }

        /// <summary>Gets or sets the concatenated SHA-1 piece digests, which are binary rather than text.</summary>
        /// <value>The piece digests.</value>
        [BencodePropertyName("pieces")]
        public byte[] Pieces { get; set; } = [];
    }
}
