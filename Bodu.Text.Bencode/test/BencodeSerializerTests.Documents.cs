// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Documents.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the read-only document object model bridge of <see cref="BencodeSerializer" />:
/// <see cref="BencodeElement" /> and <see cref="BencodeDocument" /> as members and document roots, the caller-owned
/// disposal contract of a deserialized document, the leniency flow into the bridge's re-parse, and the guards on
/// default elements and null documents.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a <see cref="BencodeElement" /> member deserializes from each Bencode value kind with the matching
    /// <see cref="BencodeValueKind" /> and re-serializes to the identical bytes.
    /// </summary>
    /// <param name="encoded">The Bencode document carrying the element value.</param>
    /// <param name="kind">The expected value kind of the deserialized element.</param>
    [TestMethod]
    [DataRow("d5:Value5:helloe", BencodeValueKind.ByteString, DisplayName = "byte string")]
    [DataRow("d5:Valuei5ee", BencodeValueKind.Integer, DisplayName = "integer")]
    [DataRow("d5:Valueli1ei2eee", BencodeValueKind.Array, DisplayName = "list")]
    [DataRow("d5:Valued1:Ai1eee", BencodeValueKind.Object, DisplayName = "dictionary")]
    public void SerializeDeserialize_WhenBencodeElementMember_ShouldPreserveKindAndRoundTrip(string encoded, BencodeValueKind kind)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(encoded);

        BencodeElement element = BencodeSerializer.Deserialize<SingleValueModel<BencodeElement>>(bytes).Value;
        Assert.AreEqual(kind, element.ValueKind);

        byte[] again = BencodeSerializer.Serialize(new SingleValueModel<BencodeElement> { Value = element });
        CollectionAssert.AreEqual(bytes, again);
    }

    /// <summary>
    /// Verifies that a deserialized <see cref="BencodeElement" /> remains readable after deserialization completes,
    /// because the bridge backs it with a non-pooled internal document that never requires disposal.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenBencodeElementMember_ShouldRemainReadableAfterwards()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valueli1ei2ei3eee");

        BencodeElement element = BencodeSerializer.Deserialize<SingleValueModel<BencodeElement>>(bytes).Value;

        Assert.AreEqual(3, element.GetArrayLength());
        Assert.AreEqual(2L, element[1].GetInt64());
    }

    /// <summary>
    /// Verifies that an element obtained from a parsed document serializes as a member, re-emitting its raw encoded
    /// form verbatim.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenBencodeElementFromParsedDocument_ShouldEmitViewedValue()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.Latin1.GetBytes("d5:Inneri5ee"));
        BencodeElement element = document.RootElement.GetProperty("Inner");

        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<BencodeElement> { Value = element });

        Assert.AreEqual("d5:Valuei5ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeDocument" /> deserializes at the document root, is owned by the caller, and
    /// re-serializes to the identical bytes.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBencodeDocumentRoot_ShouldRoundTripBytes()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:Ai1e1:B1:xe");

        using BencodeDocument document = BencodeSerializer.Deserialize<BencodeDocument>(bytes);

        Assert.AreEqual(BencodeValueKind.Object, document.RootElement.ValueKind);
        CollectionAssert.AreEqual(bytes, BencodeSerializer.Serialize(document));
    }

    /// <summary>
    /// Verifies that a scalar value deserializes into a <see cref="BencodeElement" /> at the document root and
    /// re-serializes to the identical bytes, because a Bencode document roots any value kind.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBencodeElementRootIsScalar_ShouldRoundTripBytes()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("i42e");

        BencodeElement element = BencodeSerializer.Deserialize<BencodeElement>(bytes);

        Assert.AreEqual(BencodeValueKind.Integer, element.ValueKind);
        CollectionAssert.AreEqual(bytes, BencodeSerializer.Serialize(element));
    }

    /// <summary>
    /// Verifies that the serializer's dictionary-key leniency flows into the bridge's re-parse, so an unsorted
    /// dictionary accepted by the serializer also materializes as an element.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnsortedKeysAllowed_ShouldMaterializeElement()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valued1:Bi1e1:Ai2eee");
        var options = new BencodeSerializerOptions { AllowUnsortedKeys = true };

        BencodeElement element = BencodeSerializer.Deserialize<SingleValueModel<BencodeElement>>(bytes, options).Value;

        Assert.AreEqual(2L, element.GetProperty("A").GetInt64());
    }

    /// <summary>
    /// Verifies that serializing a member whose <see cref="BencodeElement" /> is the default value throws
    /// <see cref="InvalidOperationException" />, because the element belongs to no document.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultBencodeElementMember_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = BencodeSerializer.Serialize(new SingleValueModel<BencodeElement> { Value = default });
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.WriteTo" /> on an element of a disposed document throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenBencodeElementFromDisposedDocument_ShouldThrowObjectDisposedException()
    {
        BencodeDocument document = BencodeDocument.Parse(Encoding.Latin1.GetBytes("i1e"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = BencodeSerializer.Serialize(element);
        });
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> <see cref="BencodeDocument" /> at the root throws
    /// <see cref="BencodeSerializationException" />, because Bencode has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullBencodeDocument_ShouldThrowBencodeSerializationException()
    {
        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize<BencodeDocument>(null!);
        });
    }
}
