// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Serializes a value to Bencode bytes.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that serializing an <see cref="Int128" /> outside the signed 64-bit surface or a
    /// <see cref="UInt128" /> outside the unsigned 64-bit surface throws
    /// <see cref="BencodeSerializationException" /> carrying the checked-conversion <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_When128BitExceeds64BitSurface_ShouldThrowBencodeSerializationException()
    {
        BencodeSerializationException signedEx = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(new SingleValueModel<Int128> { Value = Int128.MaxValue });
        });

        Assert.IsNotNull(signedEx.InnerException);
        Assert.IsInstanceOfType<OverflowException>(signedEx.InnerException);

        BencodeSerializationException unsignedEx = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(new SingleValueModel<UInt128> { Value = (UInt128)ulong.MaxValue + 1 });
        });

        Assert.IsNotNull(unsignedEx.InnerException);
        Assert.IsInstanceOfType<OverflowException>(unsignedEx.InnerException);
    }

    /// <summary>
    /// Verifies that a model whose member is one of the types Bencode cannot natively represent throws
    /// <see cref="NotSupportedException" /> on serialization when no converter is registered for that type.
    /// </summary>
    /// <param name="kat">The unsupported-type scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(UnsupportedTypeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenMemberTypeHasNoBuiltInConverter_ShouldThrowNotSupportedException(UnsupportedTypeKat kat)
    {
        Assert.ThrowsExactly<NotSupportedException>(kat.Serialize);
    }
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
    /// Verifies that serializing a boxed scalar typed as <see cref="object" /> at the document root dispatches to the
    /// runtime type and emits the scalar, because a Bencode document roots any value kind.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsScalar_ShouldWriteScalar()
    {
        byte[] bytes = BencodeSerializer.Serialize<object>(5);

        Assert.AreEqual("i5e", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that an element obtained from a parsed document serializes as a member, re-emitting its raw encoded
    /// form verbatim.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenBencodeElementFromParsedDocument_ShouldEmitViewedValue()
    {
        using var document = BencodeDocument.Parse(Encoding.Latin1.GetBytes("d5:Inneri5ee"));
        BencodeElement element = document.RootElement.GetProperty("Inner");

        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<BencodeElement> { Value = element });

        Assert.AreEqual("d5:Valuei5ee", Encoding.Latin1.GetString(bytes));
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
        var document = BencodeDocument.Parse(Encoding.Latin1.GetBytes("i1e"));
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

    /// <summary>
    /// Verifies that the synchronous <see cref="BencodeSerializer.Serialize{T}(Stream, T,
    /// BencodeSerializerOptions?)" /> overload writes the same canonical bytes to the stream as the in-memory
    /// overload returns.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStreamDestination_ShouldWriteCanonicalBytes()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();

        BencodeSerializer.Serialize(destination, model);

        CollectionAssert.AreEqual(BencodeSerializer.Serialize(model), destination.ToArray());
    }

    /// <summary>
    /// Verifies that the synchronous <see cref="BencodeSerializer.Serialize{T}(Stream, T,
    /// BencodeSerializerOptions?)" /> overload rejects a stream that does not support writing with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStreamNotWritable_ShouldThrowArgumentException()
    {
        using var destination = new MemoryStream([], writable: false);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            BencodeSerializer.Serialize(destination, new StreamModel());
        });
    }
    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Serialize{T}(IBufferWriter{byte}, T, BencodeSerializerOptions?)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the buffer writer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsNull_ForBufferWriterOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            BencodeSerializer.Serialize((IBufferWriter<byte>)null!, 5);
        }, "destination");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Serialize{T}(IBufferWriter{byte}, T, BencodeSerializerOptions?)" />
    /// writes the value's canonical bytes to the buffer writer when the destination is valid, confirming the guard
    /// admits the supported case.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsBufferWriter_ShouldWriteCanonicalBytes()
    {
        var buffer = new ArrayBufferWriter<byte>();

        BencodeSerializer.Serialize(buffer, 5);

        Assert.AreEqual("i5e", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

}
