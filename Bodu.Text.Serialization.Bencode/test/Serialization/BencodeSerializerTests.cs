// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Verifies the object-mapping behavior of <see cref="BencodeSerializer" />: round trips, canonical key ordering, type
/// mapping, and rejection of unsupported types.
/// </summary>
[TestClass]
public class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that an object round-trips through serialization and deserialization unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundTrip_WhenValueIsObject_ShouldPreserveMembers()
    {
        var entry = new FileEntry { Name = "ubuntu.iso", Length = 1024 };

        byte[] payload = BencodeSerializer.Serialize(entry);
        FileEntry result = BencodeSerializer.Deserialize<FileEntry>(payload);

        Assert.AreEqual("ubuntu.iso", result.Name);
        Assert.AreEqual(1024, result.Length);
    }

    /// <summary>
    /// Verifies that an object serializes to a dictionary whose keys are in ascending bytewise order, regardless of
    /// member declaration order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectHasMembers_ShouldWriteKeysInCanonicalOrder()
    {
        var entry = new FileEntry { Name = "x", Length = 5 };

        byte[] payload = BencodeSerializer.Serialize(entry);

        Assert.AreEqual("d6:Lengthi5e4:Name1:xe", Encoding.Latin1.GetString(payload));
    }

    /// <summary>
    /// Verifies that a byte array maps to a Bencode byte string and a list maps to a Bencode list.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueHasBinaryAndList_ShouldPreserveContent()
    {
        var info = new TorrentInfo
        {
            Announce = "http://tracker",
            Pieces = [0x01, 0x02, 0x03],
            Files = ["a", "b"],
        };

        byte[] payload = BencodeSerializer.Serialize(info);
        TorrentInfo result = BencodeSerializer.Deserialize<TorrentInfo>(payload);

        Assert.AreEqual("http://tracker", result.Announce);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, result.Pieces);
        CollectionAssert.AreEqual(info.Files, result.Files);
    }

    /// <summary>
    /// Verifies that a scalar integer round-trips at the document root.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsInteger_ShouldPreserveValue()
    {
        byte[] payload = BencodeSerializer.Serialize(42L);

        Assert.AreEqual("i42e", Encoding.Latin1.GetString(payload));
        Assert.AreEqual(42L, BencodeSerializer.Deserialize<long>(payload));
    }

    /// <summary>
    /// Verifies that serializing a member of a type Bencode cannot represent throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberTypeIsUnsupported_ShouldThrowNotSupportedException()
    {
        var model = new FlaggedModel { Enabled = true };

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BencodeSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that deserializing malformed bytes throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenBytesAreMalformed_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("i03e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<long>(bytes);
        });
    }

    /// <summary>
    /// Verifies that a payload produced by the serializer round-trips through a stream.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenReadingFromStream_ShouldProduceEquivalentValue()
    {
        var entry = new FileEntry { Name = "file", Length = 7 };
        byte[] payload = BencodeSerializer.Serialize(entry);

        using MemoryStream stream = new(payload);
        FileEntry result = BencodeSerializer.Deserialize<FileEntry>(stream);

        Assert.AreEqual("file", result.Name);
        Assert.AreEqual(7, result.Length);
    }
}
