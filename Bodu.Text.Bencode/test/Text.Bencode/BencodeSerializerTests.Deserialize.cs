// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Deserializes Bencode bytes to a value.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that deserializing a negative Bencode integer into a <see cref="ulong" /> member throws
    /// <see cref="BencodeSerializationException" />, matching the overflow contract of the other fixed-width integer
    /// types.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntegerIntoUInt64_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei-1ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ULongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer whose value overflows the target integer type throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    /// <param name="name">The scenario label.</param>
    /// <param name="encoded">The Bencode document to read.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("byte > 255", "d5:Valuei300ee")]
    [DataRow("byte negative", "d5:Valuei-1ee")]
    public void Deserialize_WhenIntegerOverflowsByte_ShouldThrowBencodeSerializationException(string name, string encoded)
    {
        _ = name;
        byte[] bytes = Encoding.Latin1.GetBytes(encoded);

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ByteModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer beyond <see cref="long.MaxValue" /> into a signed
    /// <see cref="long" /> member throws <see cref="BencodeFormatException" />, because the value is readable only
    /// through the unsigned surface.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerExceedsInt64RangeIntoInt64_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei18446744073709551615ee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<LongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer literal beyond <see cref="ulong.MaxValue" /> throws
    /// <see cref="BencodeFormatException" />, surfacing at the reader before the converter is consulted.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerExceedsUInt64Range_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei18446744073709551616ee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ULongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a non-byte-string token into a memory-of-byte member throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerIntoMemoryOfByte_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei5ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<SingleValueModel<Memory<byte>>>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a negative Bencode integer into a <see cref="UInt128" /> member throws
    /// <see cref="BencodeSerializationException" />, matching the overflow contract of <see cref="ulong" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntegerIntoUInt128_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei-1ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<SingleValueModel<UInt128>>(bytes);
        });
    }

    /// <summary>
    /// Verifies that a model whose member is one of the types Bencode cannot natively represent throws
    /// <see cref="NotSupportedException" /> on deserialization when no converter is registered for that type.
    /// </summary>
    /// <param name="kat">The unsupported-type scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(UnsupportedTypeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Deserialize_WhenMemberTypeHasNoBuiltInConverter_ShouldThrowNotSupportedException(UnsupportedTypeKat kat)
    {
        Assert.ThrowsExactly<NotSupportedException>(kat.Deserialize);
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
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> reads a value
    /// from a stream positioned at a canonical document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(Encoding.Latin1.GetBytes("d2:Idi7e5:Label1:xe"));

        StreamModel model = BencodeSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(byte[], BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>data</c> when the array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDataIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>((byte[])null!);
        }, "data");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNull_ForStreamOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>((Stream)null!);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the stream does not support reading.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>(source);
        }, "source");
    }

}
