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

    /// <summary>
    /// Verifies that a <see langword="null" /> reference-type member is omitted from the written dictionary while a
    /// value-type member holding zero is written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ShouldOmitKey()
    {
        var model = new NullAndZeroModel { Empty = null, Zero = 0 };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d4:Zeroi0ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> string as the document root throws
    /// <see cref="BencodeSerializationException" />, because Bencode has no null and an empty output is not a document.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsNull_ShouldThrowBencodeSerializationException()
    {
        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize<string?>(null);
        });
    }

    /// <summary>
    /// Verifies that serializing a <see cref="double" />, which has no Bencode form, throws
    /// <see cref="NotSupportedException" /> rather than writing a truncated integer.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsDouble_ShouldThrowNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BencodeSerializer.Serialize(42.23);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="ulong" /> member above <see cref="long.MaxValue" /> is written with all its digits,
    /// not as the negative number a cast through <see cref="long" /> would give.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenUInt64AboveInt64Max_ShouldWriteAllDigits()
    {
        var model = new ULongModel { Value = ulong.MaxValue };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d5:Valuei18446744073709551615ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that serializing a <see cref="char" /> value, which has no built-in converter, throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsChar_ShouldThrowNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BencodeSerializer.Serialize('a');
        });
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> object as the document root throws
    /// <see cref="BencodeSerializationException" /> rather than returning an empty array, which no reader accepts.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenClassValueIsNull_ShouldThrowBencodeSerializationException()
    {
        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize<StreamModel?>(null);
        });
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> root value to a buffer writer throws
    /// <see cref="BencodeSerializationException" /> and writes nothing.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsNull_ForBufferWriterOverload_ShouldThrowBencodeSerializationException()
    {
        var destination = new ArrayBufferWriter<byte>();

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            BencodeSerializer.Serialize<string?>(destination, null);
        });

        Assert.AreEqual(0, destination.WrittenCount);
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> root value to a stream throws
    /// <see cref="BencodeSerializationException" /> and writes nothing.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsNull_ForStreamOverload_ShouldThrowBencodeSerializationException()
    {
        using var destination = new MemoryStream();

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            BencodeSerializer.Serialize<int?>(destination, null);
        });

        Assert.AreEqual(0L, destination.Length);
    }

    /// <summary>
    /// Verifies that a public field a derived type hides with <see langword="new" /> and a field of another type is
    /// written once, from the derived declaration, rather than failing as two members mapped to one key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldIsHiddenWithNew_ShouldWriteTheDerivedField()
    {
        string text = Encoding.Latin1.GetString(BencodeSerializer.Serialize(new HiddenFieldDerivedModel(), new BencodeSerializerOptions { IncludeFields = true }));

        Assert.AreEqual("d5:Value7:derivede", text);
    }

    /// <summary>
    /// Verifies that a property a derived type hides with <see langword="new" /> and a property of another type is
    /// written once, from the derived declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyIsHiddenByAnotherType_ShouldWriteTheDerivedProperty()
    {
        string text = Encoding.Latin1.GetString(BencodeSerializer.Serialize(new HiddenPropertyDerivedModel()));

        Assert.AreEqual("d5:Value7:derivede", text);
    }

    /// <summary>
    /// Verifies that a property hidden at each of two levels of derivation is written once, from the most derived
    /// declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsHiddenTwoLevelsDown_ShouldWriteTheMostDerivedMember()
    {
        string text = Encoding.Latin1.GetString(BencodeSerializer.Serialize(new HiddenTwiceDerivedModel()));

        Assert.AreEqual("d5:Value7:derivede", text);
    }
}
