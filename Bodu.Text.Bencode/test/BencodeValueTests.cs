// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeValue" /> scalar surface: the <c>Create</c> factory overloads, checked integer
/// conversions through <see cref="BencodeValue.GetValue{T}" /> and <see cref="BencodeValue.TryGetValue{T}" />,
/// defensive copying of byte-string payloads, and canonical serialization of both scalar kinds.
/// </summary>
[TestClass]
public class BencodeValueTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(long)" /> produces an integer-kind value readable as
    /// <see cref="long" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenInt64_ShouldReportIntegerKind()
    {
        BencodeValue value = BencodeValue.Create(long.MaxValue);

        Assert.AreEqual(BencodeValueKind.Integer, value.GetValueKind());
        Assert.AreEqual(long.MaxValue, value.GetValue<long>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(int)" /> stores the value as a 64-bit integer.
    /// </summary>
    [TestMethod]
    public void Create_WhenInt32_ShouldStoreAsInteger()
    {
        BencodeValue value = BencodeValue.Create(42);

        Assert.AreEqual(BencodeValueKind.Integer, value.GetValueKind());
        Assert.AreEqual(42L, value.GetValue<long>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(string)" /> produces a byte-string-kind value readable as
    /// <see cref="string" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenString_ShouldReportByteStringKind()
    {
        BencodeValue value = BencodeValue.Create("héllo");

        Assert.AreEqual(BencodeValueKind.ByteString, value.GetValueKind());
        Assert.AreEqual("héllo", value.GetValue<string>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(string)" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>value</c> when the string is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenStringIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeValue.Create((string)null!);
        }, "value");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(byte[])" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>value</c> when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenByteArrayIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeValue.Create((byte[])null!);
        }, "value");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(byte[])" /> copies the supplied array, so mutating the source
    /// afterwards does not change the stored payload.
    /// </summary>
    [TestMethod]
    public void Create_WhenByteArray_ShouldCopyContent()
    {
        byte[] source = [1, 2, 3];
        BencodeValue value = BencodeValue.Create(source);

        source[0] = 9;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, value.GetValue<byte[]>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.Create(ReadOnlySpan{byte})" /> copies the supplied bytes.
    /// </summary>
    [TestMethod]
    public void Create_WhenSpan_ShouldCopyContent()
    {
        byte[] source = [0x00, 0x80, 0xFF];
        BencodeValue value = BencodeValue.Create(source.AsSpan());

        source[1] = 0;

        CollectionAssert.AreEqual(new byte[] { 0x00, 0x80, 0xFF }, value.GetValue<byte[]>());
    }

    /// <summary>
    /// Verifies that an in-range integer converts to every fixed-width integer type through
    /// <see cref="BencodeValue.GetValue{T}" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerInRangeOfEveryWidth_ShouldConvert()
    {
        BencodeValue value = BencodeValue.Create(42L);

        Assert.AreEqual((sbyte)42, value.GetValue<sbyte>());
        Assert.AreEqual((byte)42, value.GetValue<byte>());
        Assert.AreEqual((short)42, value.GetValue<short>());
        Assert.AreEqual((ushort)42, value.GetValue<ushort>());
        Assert.AreEqual(42, value.GetValue<int>());
        Assert.AreEqual(42U, value.GetValue<uint>());
        Assert.AreEqual(42L, value.GetValue<long>());
        Assert.AreEqual(42UL, value.GetValue<ulong>());
    }

    /// <summary>
    /// Verifies that a negative integer read as <see cref="ulong" /> throws
    /// <see cref="InvalidOperationException" />, because the checked conversion is out of range.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenNegativeIntegerReadAsUInt64_ShouldThrowInvalidOperationException()
    {
        BencodeValue value = BencodeValue.Create(-1L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<ulong>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.TryGetValue{T}" /> returns <see langword="false" /> when an integer is
    /// requested as a type with no Bencode integer mapping.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenIntegerReadAsUnsupportedType_ShouldReturnFalse()
    {
        BencodeValue value = BencodeValue.Create(1L);

        Assert.IsFalse(value.TryGetValue(out bool asBoolean));
        Assert.IsFalse(asBoolean);
        Assert.IsFalse(value.TryGetValue(out double asDouble));
        Assert.AreEqual(0d, asDouble);
    }

    /// <summary>
    /// Verifies that reading a byte string as a <see cref="byte" /> array returns an independent copy of the payload.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenByteStringReadAsByteArray_ShouldReturnCopy()
    {
        BencodeValue value = BencodeValue.Create(new byte[] { 1, 2, 3 });

        Assert.IsTrue(value.TryGetValue(out byte[] first));
        first[0] = 9;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, value.GetValue<byte[]>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.ToString" /> renders an integer as invariant decimal text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenInteger_ShouldRenderInvariantDecimal()
    {
        Assert.AreEqual("-42", BencodeValue.Create(-42L).ToString());
        Assert.AreEqual("0", BencodeValue.Create(0L).ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.ToString" /> renders a byte string as UTF-8 text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenByteString_ShouldRenderUtf8Text()
    {
        Assert.AreEqual("héllo", BencodeValue.Create("héllo").ToString());
    }

    /// <summary>
    /// Verifies that integers serialize to the canonical <c>i…e</c> token.
    /// </summary>
    /// <param name="value">The integer payload.</param>
    /// <param name="expected">The expected canonical encoding.</param>
    [TestMethod]
    [DataRow(42L, "i42e")]
    [DataRow(-7L, "i-7e")]
    [DataRow(0L, "i0e")]
    public void ToByteArray_WhenInteger_ShouldEmitIntegerToken(long value, string expected)
    {
        byte[] bytes = BencodeValue.Create(value).ToByteArray();

        Assert.AreEqual(expected, Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that an empty string serializes to the canonical zero-length byte string <c>0:</c>.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenEmptyString_ShouldEmitZeroLengthByteString()
    {
        byte[] bytes = BencodeValue.Create(string.Empty).ToByteArray();

        Assert.AreEqual("0:", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.DeepClone" /> copies the byte-string payload into an independent,
    /// parentless node.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenByteString_ShouldCopyPayload()
    {
        var owner = new BencodeArray();
        BencodeValue original = BencodeValue.Create(new byte[] { 1, 2, 3 });
        owner.Add(original);

        BencodeValue clone = original.DeepClone().AsValue();

        Assert.IsNull(clone.Parent);
        CollectionAssert.AreEqual(original.GetValue<byte[]>(), clone.GetValue<byte[]>());
    }
}
