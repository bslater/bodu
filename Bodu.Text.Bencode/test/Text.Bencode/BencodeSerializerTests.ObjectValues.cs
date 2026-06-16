// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.ObjectValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="object" />-typed member support of <see cref="BencodeSerializer" />: on write the runtime
/// type selects the converter, a bare <see cref="object" /> emits an empty dictionary, and a <see langword="null" />
/// member is omitted; on read the value surfaces as a <see cref="BencodeElement" />, mirroring the
/// <see cref="System.Text.Json.JsonElement" /> behavior of <see cref="System.Text.Json.JsonSerializer" />.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that an <see cref="object" />-typed member serializes through its runtime type's converter across the
    /// representative shapes.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsBoxedValue_ShouldDispatchToRuntimeType()
    {
        Assert.AreEqual(
            "d5:Value5:helloe",
            Encoding.Latin1.GetString(BencodeSerializer.Serialize(new SingleValueModel<object> { Value = "hello" })));

        Assert.AreEqual(
            "d5:Valuei5ee",
            Encoding.Latin1.GetString(BencodeSerializer.Serialize(new SingleValueModel<object> { Value = 5 })));

        Assert.AreEqual(
            "d5:Valueli1ei2eee",
            Encoding.Latin1.GetString(BencodeSerializer.Serialize(new SingleValueModel<object> { Value = new[] { 1, 2 } })));

        Assert.AreEqual(
            "d5:Valued1:Ai1eee",
            Encoding.Latin1.GetString(BencodeSerializer.Serialize(new SingleValueModel<object> { Value = new Dictionary<string, int> { ["A"] = 1 } })));
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a bare <see cref="object" /> serializes as an empty
    /// dictionary, the Bencode analogue of the empty JSON object.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsBareObject_ShouldWriteEmptyDictionary()
    {
        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = new object() });

        Assert.AreEqual("d5:Valuedee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member whose value is <see langword="null" /> is omitted from the
    /// output, because Bencode has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberNull_ShouldOmitMember()
    {
        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = null });

        Assert.AreEqual("de", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing into an <see cref="object" />-typed member yields a <see cref="BencodeElement" />
    /// carrying the matching <see cref="BencodeValueKind" /> for each Bencode value kind.
    /// </summary>
    /// <param name="encoded">The Bencode document carrying the value.</param>
    /// <param name="kind">The expected value kind of the surfaced element.</param>
    [TestMethod]
    [DataRow("d5:Value5:helloe", BencodeValueKind.ByteString, DisplayName = "byte string")]
    [DataRow("d5:Valuei5ee", BencodeValueKind.Integer, DisplayName = "integer")]
    [DataRow("d5:Valueli1eee", BencodeValueKind.Array, DisplayName = "list")]
    [DataRow("d5:Valued1:Ai1eee", BencodeValueKind.Object, DisplayName = "dictionary")]
    public void Deserialize_WhenObjectMember_ShouldSurfaceBencodeElement(string encoded, BencodeValueKind kind)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(encoded);

        object actual = BencodeSerializer.Deserialize<SingleValueModel<object>>(bytes).Value!;

        Assert.IsInstanceOfType<BencodeElement>(actual);
        Assert.AreEqual(kind, ((BencodeElement)actual).ValueKind);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member round-trips: the element read back re-serializes to the
    /// bytes its source value produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenObjectMember_ShouldRoundTripThroughElement()
    {
        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = new[] { 1, 2, 3 } });
        object element = BencodeSerializer.Deserialize<SingleValueModel<object>>(bytes).Value!;
        byte[] again = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = element });

        CollectionAssert.AreEqual(bytes, again);
    }

    /// <summary>
    /// Verifies that deserializing a whole document into <see cref="object" /> yields a dictionary-kind
    /// <see cref="BencodeElement" /> exposing the document's properties.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenObjectRoot_ShouldSurfaceDictionaryElement()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:Ai1ee");

        object actual = BencodeSerializer.Deserialize<object>(bytes);

        Assert.IsInstanceOfType<BencodeElement>(actual);

        var element = (BencodeElement)actual;
        Assert.AreEqual(BencodeValueKind.Object, element.ValueKind);
        Assert.AreEqual(1L, element.GetProperty("A").GetInt64());
    }

    /// <summary>
    /// Verifies that serializing a boxed scalar typed as <see cref="object" /> at the document root dispatches to the
    /// runtime type and emits the scalar, because a Bencode document roots any value kind.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsScalar_ShouldWriteScalar()
    {
        byte[] bytes = BencodeSerializer.Serialize<object>(5);

        Assert.AreEqual("i5e", Encoding.Latin1.GetString(bytes));
    }
}
