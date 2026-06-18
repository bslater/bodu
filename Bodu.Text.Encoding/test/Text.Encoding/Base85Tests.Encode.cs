// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that the byte-array and span overloads produce identical output across various input sizes.
    /// </summary>
    /// <param name="size">The input size.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(32)]
    public void Encode_ByteArrayAndSpanOverloads_ShouldProduceIdenticalOutput(int size)
    {
        byte[] bytes = new byte[size];
        for (int i = 0; i < size; i++)
        {
            bytes[i] = (byte)((i * 11) ^ 0x9C);
        }

        string fromArray = Base85.Encode(bytes);
        string fromSpan = Base85.Encode(bytes.AsSpan());

        Assert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Base85Variant)" /> reproduces every Adobe Ascii85
    /// Known Answer Test vector, including the <c>z</c> shortcut for all-zero groups.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Ascii85Vectors), typeof(Base85KnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForAscii85KnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = Base85.Encode(vector.DecodedBytes, Base85Variant.Ascii85);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Base85Variant)" /> reproduces every Z85 Known
    /// Answer Test vector, including the RFC 32 reference <c>HelloWorld</c> example.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Z85Vectors), typeof(Base85KnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForZ85KnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = Base85.Encode(vector.DecodedBytes, Base85Variant.Z85);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant emits
    /// (1 + remaining) characters for a partial trailing group.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedCharCount">The expected encoded character count.</param>
    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 4)]
    [DataRow(5, 7)]
    [DataRow(8, 10)]
    [DataRow(9, 12)]
    public void Encode_WhenAscii85PartialGroup_ShouldEmitExpectedLength(int byteCount, int expectedCharCount)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            bytes[i] = (byte)(i + 1);

        string actual = Base85.Encode(bytes);

        Assert.AreEqual(expectedCharCount, actual.Length);
    }
    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant produces the
    /// expected 5-character output for a complete 4-byte group ("Hell").
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85VariantCompleteGroup_ShouldReturnFiveCharacters()
    {
        byte[] bytes = Ascii("Hell");

        string actual = Base85.Encode(bytes);

        Assert.AreEqual(5, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant emits the <c>z</c>
    /// shortcut for a 4-zero-byte input.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85VariantFourZeroBytes_ShouldEmitZShortcut()
    {
        byte[] bytes = new byte[4];

        string actual = Base85.Encode(bytes);

        Assert.AreEqual("z", actual);
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant)" /> with a
    /// destination sized for the actual <c>z</c>-shortcut output succeeds rather than throwing.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85ZShortcutAndDestinationExactlyActualSize_ShouldSucceed()
    {
        byte[] zeros = new byte[4];
        char[] destination = new char[1];

        int charsWritten = Base85.Encode(zeros.AsSpan(), destination);

        Assert.AreEqual(1, charsWritten);
        Assert.AreEqual('z', destination[0]);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], int, int, Base85Variant)" /> rejects a count that exceeds the
    /// array length.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCountExceedsArrayLength_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.Encode(new byte[4], 0, 100);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant)" /> throws when the
    /// destination is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        byte[] bytes = new byte[8];
        char[] destination = new char[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode(bytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], int, int, Base85Variant)" /> rejects a negative offset.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNegativeOffset_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.Encode(new byte[4], -1, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], int, int, Base85Variant)" /> only encodes the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        byte[] bytes = Ascii("xxxxHelloyyy");

        string actual = Base85.Encode(bytes, 4, 5);

        string expected = Base85.Encode(Ascii("Hello"));
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> rejects an undefined variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.Encode(new byte[4], (Base85Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Z85 variant emits 5 characters
    /// per 4-byte group.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85VariantAlignedInput_ShouldEmitFiveCharsPerFourBytes()
    {
        byte[] bytes = new byte[8];

        string actual = Base85.Encode(bytes, Base85Variant.Z85);

        Assert.AreEqual(10, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Z85 variant rejects non-aligned
    /// input.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85VariantAndNonAlignedInput_ShouldThrowExactly()
    {
        byte[] bytes = new byte[5];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode(bytes, Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Regression: verifies that the renamed exact <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant)" />
    /// accounts for the Ascii85 <c>z</c> shortcut — four-zero groups collapse to a single character.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_ForAscii85WithZeroGroups_ShouldAccountForShortcut()
    {
        // 8 zero bytes = two all-zero groups = "zz" (2 chars), not 10.
        Assert.AreEqual(2, Base85.GetEncodedLength(new byte[8].AsSpan(), Base85Variant.Ascii85));

        // 4 zero bytes + 4 non-zero bytes (high byte set) = "z" + 5 chars = 6.
        byte[] mixed = new byte[8];
        mixed[4] = 0x01;
        Assert.AreEqual(6, Base85.GetEncodedLength(mixed.AsSpan(), Base85Variant.Ascii85));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant)" /> agrees
    /// with the actual encoded length across input shapes, so consumers can pre-size buffers exactly.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(12)]
    public void GetEncodedLength_ShouldAgreeWithActualEncodedLength(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = (byte)((i * 7) + 1);
        }

        int predicted = Base85.GetEncodedLength(bytes.AsSpan(), Base85Variant.Ascii85);
        int actual = Base85.Encode(bytes, Base85Variant.Ascii85).Length;

        Assert.AreEqual(predicted, actual);
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.TryEncode" /> for Ascii85 succeeds when the destination is sized
    /// for the actual output that uses the <c>z</c> shortcut, even though it is smaller than the worst-case upper
    /// bound. Before the fix, a tightly-sized destination would be rejected because the check compared against
    /// the no-shortcut maximum length.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenAscii85ZShortcutShrinksOutput_ShouldSucceedWithTightlySizedDestination()
    {
        // Four zero bytes encode as "z" (1 char) under Ascii85, well below the 5-char worst case.
        byte[] zeros = new byte[4];
        char[] destination = new char[1];

        bool ok = Base85.TryEncode(zeros.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok, "TryEncode should succeed when destination is at least the actual encoded length.");
        Assert.AreEqual(1, charsWritten);
        Assert.AreEqual("z", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode" /> succeeds when the destination is exactly the required size.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedCharCount">The expected output char count for the Ascii85 variant.</param>
    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 4)]
    [DataRow(4, 5)]
    [DataRow(5, 7)]
    [DataRow(8, 10)]
    public void TryEncode_WhenDestinationExactlyRequiredSize_ShouldReturnTrueAndFillBuffer(int byteCount, int expectedCharCount)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = (byte)(i + 1); // avoid all-zero so 'z' shortcut isn't taken
        }

        char[] destination = new char[expectedCharCount];

        bool ok = Base85.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expectedCharCount, charsWritten);
    }

}
