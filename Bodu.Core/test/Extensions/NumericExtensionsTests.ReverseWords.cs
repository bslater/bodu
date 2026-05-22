// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.ReverseWords.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseWords(uint)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U)]
    [DataRow(0x00000001U)]
    [DataRow(0xAABBCCDDU)]
    [DataRow(0xFFFFFFFFU)]
    public void ReverseWords_WhenAppliedTwice_ForUInt_ShouldReturnOriginalValue(uint value) =>
        Assert.AreEqual(value, value.ReverseWords().ReverseWords());

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseWords(ulong)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL)]
    [DataRow(0x0000000000000001UL)]
    [DataRow(0xAABBCCDD11223344UL)]
    [DataRow(0xFFFFFFFFFFFFFFFFUL)]
    public void ReverseWords_WhenAppliedTwice_ForULong_ShouldReturnOriginalValue(ulong value) =>
        Assert.AreEqual(value, value.ReverseWords().ReverseWords());

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseWords(ushort)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0001)]
    [DataRow((ushort)0x1234)]
    [DataRow((ushort)0xFFFF)]
    public void ReverseWords_WhenAppliedTwice_ForUShort_ShouldReturnOriginalValue(ushort value) =>
        Assert.AreEqual(value, value.ReverseWords().ReverseWords());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(byte[])" /> throws
    /// <see cref="ArgumentException" /> when the input array is empty (length 0 is not a positive multiple of 2).
    /// </summary>
    [TestMethod]
    public void ReverseWords_WhenByteArrayIsEmpty_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Array.Empty<byte>().ReverseWords();
        });

    // --------------------------------------------------
    // byte[] — exception cases
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ReverseWords_WhenByteArrayIsNull_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((byte[])null!).ReverseWords();
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(byte[])" /> does not modify the original
    /// array — the result is a distinct new allocation.
    /// </summary>
    [TestMethod]
    public void ReverseWords_WhenByteArrayIsValid_ShouldNotMutateOriginalArray()
    {
        byte[] input = [0xAA, 0xBB, 0xCC, 0xDD];
        var snapshot = (byte[])input.Clone();

        _ = input.ReverseWords();

        CollectionAssert.AreEqual(snapshot, input);
    }

    // --------------------------------------------------
    // byte[] — valid inputs; each adjacent pair is swapped
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(byte[])" /> returns a new array in which
    /// each adjacent pair of bytes has been swapped, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(new byte[] { 0xAA, 0xBB }, new byte[] { 0xBB, 0xAA })]
    [DataRow(new byte[] { 0x00, 0x01 }, new byte[] { 0x01, 0x00 })]
    [DataRow(new byte[] { 0x00, 0x00 }, new byte[] { 0x00, 0x00 })]
    [DataRow(new byte[] { 0xFF, 0xFF }, new byte[] { 0xFF, 0xFF })]
    [DataRow(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, new byte[] { 0xBB, 0xAA, 0xDD, 0xCC })]
    [DataRow(new byte[] { 0x12, 0x34, 0x56, 0x78 }, new byte[] { 0x34, 0x12, 0x78, 0x56 })]
    [DataRow(
        new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 },
        new byte[] { 0x02, 0x01, 0x04, 0x03, 0x06, 0x05 })]
    public void ReverseWords_WhenByteArrayIsValid_ShouldSwapEachAdjacentPair(byte[] bytes, byte[] expected)
    {
        var actual = bytes.ReverseWords();

        Trace.WriteLineIf(!actual.AsSpan().SequenceEqual(expected), $"Input   : {BitConverter.ToString(bytes)}");
        Trace.WriteLineIf(!actual.AsSpan().SequenceEqual(expected), $"Expected: {BitConverter.ToString(expected)}");
        Trace.WriteLineIf(!actual.AsSpan().SequenceEqual(expected), $"Actual  : {BitConverter.ToString(actual)}");

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(byte[])" /> throws
    /// <see cref="ArgumentException" /> when the array length is not a positive multiple of two.
    /// </summary>
    [TestMethod]
    [DataRow(new byte[] { 0x01 })]          // length 1 — odd
    [DataRow(new byte[] { 0x01, 0x02, 0x03 })] // length 3 — odd
    public void ReverseWords_WhenByteArrayLengthIsOdd_ShouldThrowExactly(byte[] bytes) =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            bytes.ReverseWords();
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(Span{byte})" /> throws
    /// <see cref="ArgumentException" /> when the span is empty.
    /// </summary>
    [TestMethod]
    public void ReverseWords_WhenSpanIsEmpty_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte>.Empty.ReverseWords();
        });

    // --------------------------------------------------
    // Span<byte> — valid inputs; in-place swap
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(Span{byte})" /> modifies the span in place
    /// by swapping each adjacent pair of bytes.
    /// </summary>
    [TestMethod]
    [DataRow(new byte[] { 0xAA, 0xBB }, new byte[] { 0xBB, 0xAA })]
    [DataRow(new byte[] { 0x00, 0x01 }, new byte[] { 0x01, 0x00 })]
    [DataRow(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, new byte[] { 0xBB, 0xAA, 0xDD, 0xCC })]
    [DataRow(new byte[] { 0x12, 0x34, 0x56, 0x78 }, new byte[] { 0x34, 0x12, 0x78, 0x56 })]
    public void ReverseWords_WhenSpanIsValid_ShouldSwapEachAdjacentPairInPlace(byte[] input, byte[] expected)
    {
        Span<byte> span = input.AsSpan();
        span.ReverseWords();
        CollectionAssert.AreEqual(expected, input);
    }

    // --------------------------------------------------
    // Span<byte> — exception cases
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(Span{byte})" /> throws
    /// <see cref="ArgumentException" /> when the span length is not a positive multiple of two.
    /// </summary>
    [TestMethod]
    [DataRow(new byte[] { 0x01 })]
    [DataRow(new byte[] { 0x01, 0x02, 0x03 })]
    public void ReverseWords_WhenSpanLengthIsOdd_ShouldThrowExactly(byte[] bytes) =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            bytes.AsSpan().ReverseWords();
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(uint)" /> is distinct from
    /// <see cref="NumericExtensions.ReverseBytes(uint)" /> for values where the two operations differ.
    /// </summary>
    [TestMethod]
    [DataRow(0xAABBCCDDU, 0xDDCCBBAAU, 0xBBAADDCCU)]
    [DataRow(0x12345678U, 0x78563412U, 0x34127856U)]
    public void ReverseWords_WhenValueIsUInt_ShouldDifferFromReverseBytes(
        uint value, uint expectedReverseBytes, uint expectedReverseWords)
    {
        Assert.AreEqual(expectedReverseBytes, value.ReverseBytes(), "ReverseBytes reference");
        Assert.AreEqual(expectedReverseWords, value.ReverseWords(), "ReverseWords result");
        Assert.AreNotEqual(value.ReverseBytes(), value.ReverseWords(), "Operations should differ for this input");
    }

    // --------------------------------------------------
    // uint — two 16-bit words; bytes swapped within each word, word order preserved
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(uint)" /> swaps the two bytes within each
    /// 16-bit half of the input value, leaving the word order unchanged.
    /// For example, <c>0xAABBCCDD</c> → <c>0xBBAADDCC</c>, not <c>0xDDCCBBAA</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, 0x00000000U, "all bits zero — identity")]
    [DataRow(0xFFFFFFFFU, 0xFFFFFFFFU, "all bits set — identity")]
    [DataRow(0x00FF00FFU, 0xFF00FF00U, "byte-swapped palindrome")]
    [DataRow(0xAABBCCDDU, 0xBBAADDCCU, "AABB|CCDD → BBAA|DDCC (word order preserved)")]
    [DataRow(0x12345678U, 0x34127856U, "1234|5678 → 3412|7856")]
    [DataRow(0x01020304U, 0x02010403U, "0102|0304 → 0201|0403")]
    [DataRow(0x00010002U, 0x01000200U, "single bits in each word")]
    [DataRow(0xFF00FF00U, 0x00FF00FFU, "high bytes only in each word")]
    public void ReverseWords_WhenValueIsUInt_ShouldSwapBytesWithinEachWord(uint value, uint expected, string description)
    {
        var actual = value.ReverseWords();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X8}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X8}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X8}");

        Assert.AreEqual(expected, actual, description);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(ulong)" /> is distinct from
    /// <see cref="NumericExtensions.ReverseBytes(ulong)" /> for values where the two operations differ.
    /// </summary>
    [TestMethod]
    [DataRow(0xAABBCCDD11223344UL, 0x44332211DDCCBBAAUL, 0xBBAADDCC22114433UL)]
    [DataRow(0x0102030405060708UL, 0x0807060504030201UL, 0x0201040306050807UL)]
    public void ReverseWords_WhenValueIsULong_ShouldDifferFromReverseBytes(
        ulong value, ulong expectedReverseBytes, ulong expectedReverseWords)
    {
        Assert.AreEqual(expectedReverseBytes, value.ReverseBytes(), "ReverseBytes reference");
        Assert.AreEqual(expectedReverseWords, value.ReverseWords(), "ReverseWords result");
        Assert.AreNotEqual(value.ReverseBytes(), value.ReverseWords(), "Operations should differ for this input");
    }

    // --------------------------------------------------
    // ulong — four 16-bit words; bytes swapped within each word, word order preserved
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(ulong)" /> swaps the two bytes within each
    /// 16-bit quarter of the input value, leaving the word order unchanged.
    /// For example, <c>0xAABBCCDD11223344</c> → <c>0xBBAADDCC22114433</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, 0x0000000000000000UL, "all bits zero — identity")]
    [DataRow(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, "all bits set — identity")]
    [DataRow(0x00FF00FF00FF00FFUL, 0xFF00FF00FF00FF00UL, "byte-swapped palindrome")]
    [DataRow(0xAABBCCDD11223344UL, 0xBBAADDCC22114433UL, "AABB|CCDD|1122|3344 → BBAA|DDCC|2211|4433")]
    [DataRow(0x0102030405060708UL, 0x0201040306050807UL, "sequential bytes — each pair swapped")]
    [DataRow(0x0001000200030004UL, 0x0100020003000400UL, "single bits in each word")]
    [DataRow(0xFF00FF00FF00FF00UL, 0x00FF00FF00FF00FFUL, "high bytes only in each word")]
    public void ReverseWords_WhenValueIsULong_ShouldSwapBytesWithinEachWord(ulong value, ulong expected, string description)
    {
        var actual = value.ReverseWords();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X16}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X16}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X16}");

        Assert.AreEqual(expected, actual, description);
    }
    // --------------------------------------------------
    // ushort — a single 16-bit word; result is identical to ReverseBytes(ushort)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseWords(ushort)" /> swaps the two bytes of the
    /// input value, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, (ushort)0x0000, "all bits zero — identity")]
    [DataRow((ushort)0xFFFF, (ushort)0xFFFF, "all bits set — identity")]
    [DataRow((ushort)0xAAAA, (ushort)0xAAAA, "byte-palindrome — identity")]
    [DataRow((ushort)0xAABB, (ushort)0xBBAA, "AABB → BBAA")]
    [DataRow((ushort)0x1234, (ushort)0x3412, "1234 → 3412")]
    [DataRow((ushort)0x0100, (ushort)0x0001, "single bit in high byte → low byte")]
    [DataRow((ushort)0x0001, (ushort)0x0100, "single bit in low byte → high byte")]
    public void ReverseWords_WhenValueIsUShort_ShouldSwapBytes(ushort value, ushort expected, string description)
    {
        var actual = value.ReverseWords();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X4}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X4}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X4}");

        Assert.AreEqual(expected, actual, description);
    }

}