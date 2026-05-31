// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.ReverseBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBytes(uint)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U)]
    [DataRow(0x00000001U)]
    [DataRow(0x12345678U)]
    [DataRow(0xFFFFFFFFU)]
    public void ReverseBytes_WhenAppliedTwice_ForUInt_ShouldReturnOriginalValue(uint value) =>
        Assert.AreEqual(value, value.ReverseBytes().ReverseBytes());

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBytes(ulong)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL)]
    [DataRow(0x0000000000000001UL)]
    [DataRow(0x0102030405060708UL)]
    [DataRow(0xFFFFFFFFFFFFFFFFUL)]
    public void ReverseBytes_WhenAppliedTwice_ForULong_ShouldReturnOriginalValue(ulong value) =>
        Assert.AreEqual(value, value.ReverseBytes().ReverseBytes());

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBytes(ushort)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0001)]
    [DataRow((ushort)0x1234)]
    [DataRow((ushort)0xFFFF)]
    public void ReverseBytes_WhenAppliedTwice_ForUShort_ShouldReturnOriginalValue(ushort value) =>
        Assert.AreEqual(value, value.ReverseBytes().ReverseBytes());

    // --------------------------------------------------
    // uint
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytes(uint)" /> reverses the four bytes of the
    /// input value, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, 0x00000000U, "all bits zero — identity")]
    [DataRow(0xFFFFFFFFU, 0xFFFFFFFFU, "all bits set — identity")]
    [DataRow(0x55555555U, 0x55555555U, "byte-palindrome — identity")]
    [DataRow(0x12345678U, 0x78563412U, "12345678 → 78563412")]
    [DataRow(0xABCD1234U, 0x3412CDABU, "ABCD1234 → 3412CDAB")]
    [DataRow(0x01020304U, 0x04030201U, "01020304 → 04030201")]
    [DataRow(0x00000001U, 0x01000000U, "single LSB → single MSB")]
    [DataRow(0x01000000U, 0x00000001U, "single MSB → single LSB")]
    public void ReverseBytes_WhenValueIsUInt_ShouldReverseByteOrder(uint value, uint expected, string description)
    {
        var actual = value.ReverseBytes();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X8}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X8}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X8}");

        Assert.AreEqual(expected, actual, description);
    }

    // --------------------------------------------------
    // ulong
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytes(ulong)" /> reverses the eight bytes of the
    /// input value, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, 0x0000000000000000UL, "all bits zero — identity")]
    [DataRow(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, "all bits set — identity")]
    [DataRow(0x5555555555555555UL, 0x5555555555555555UL, "byte-palindrome — identity")]
    [DataRow(0x0102030405060708UL, 0x0807060504030201UL, "sequential byte values")]
    [DataRow(0x1234567890ABCDEFUL, 0xEFCDAB9078563412UL, "arbitrary non-palindromic value")]
    [DataRow(0xABCD1234567890ABUL, 0xAB9078563412CDABUL, "split arbitrary value")]
    [DataRow(0x0000000000000001UL, 0x0100000000000000UL, "single LSB → byte 7")]
    [DataRow(0x0100000000000000UL, 0x0000000000000001UL, "byte 7 → single LSB")]
    public void ReverseBytes_WhenValueIsULong_ShouldReverseByteOrder(ulong value, ulong expected, string description)
    {
        var actual = value.ReverseBytes();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X16}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X16}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X16}");

        Assert.AreEqual(expected, actual, description);
    }
    // --------------------------------------------------
    // ushort
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytes(ushort)" /> swaps the two bytes of the
    /// input value, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, (ushort)0x0000, "all bits zero — identity")]
    [DataRow((ushort)0xFFFF, (ushort)0xFFFF, "all bits set — identity")]
    [DataRow((ushort)0xAAAA, (ushort)0xAAAA, "byte-palindrome — identity")]
    [DataRow((ushort)0x1234, (ushort)0x3412, "arbitrary non-palindromic value")]
    [DataRow((ushort)0xABCD, (ushort)0xCDAB, "ABCD → CDAB")]
    [DataRow((ushort)0x0102, (ushort)0x0201, "0102 → 0201")]
    [DataRow((ushort)0x0100, (ushort)0x0001, "single bit in high byte → low byte")]
    [DataRow((ushort)0x0001, (ushort)0x0100, "single bit in low byte → high byte")]
    public void ReverseBytes_WhenValueIsUShort_ShouldSwapBytes(ushort value, ushort expected, string description)
    {
        var actual = value.ReverseBytes();

        Trace.WriteLineIf(actual != expected, $"[{description}]");
        Trace.WriteLineIf(actual != expected, $"value   : {value:X4}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X4}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X4}");

        Assert.AreEqual(expected, actual, description);
    }

}