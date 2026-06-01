// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LengthHelperOverflowTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Regression tests for the overflow protection added to every <c>GetEncodedLength</c> / <c>GetMaxEncodedLength</c>
/// / <c>GetMaxDecodedLength</c> helper. Before these checks the helpers silently wrapped around and returned a
/// negative or otherwise nonsensical value when the input was close to <see cref="int.MaxValue" />, leading
/// downstream allocators to receive bad sizes.
/// </summary>
[TestClass]
public sealed class LengthHelperOverflowTests
{

    /// <summary>
    /// Verifies that valid (non-overflowing) byte counts still resolve correctly after the overflow check is added.
    /// </summary>
    [TestMethod]
    public void AllEncodings_GetLengthHelpers_ShouldStillReturnExpectedValuesForValidInput()
    {
        // Sanity-check a non-trivial size for each encoding.
        Assert.AreEqual(2_000_000, Base16.GetEncodedLength(1_000_000));
        Assert.AreEqual(1_600_000, Base32.GetEncodedLength(1_000_000));
        Assert.AreEqual(1_333_336, Base64.GetEncodedLength(1_000_000));
        Assert.AreEqual(1_380_001, Base58.GetMaxEncodedLength(1_000_000));
        Assert.AreEqual(1_250_000, Base85.GetMaxEncodedLength(1_000_000, Base85Variant.Ascii85));
    }
    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength(int, BaseFormattingOptions)" /> throws
    /// <see cref="OverflowException" /> when the result would not fit in an <see cref="int" />.
    /// </summary>
    [TestMethod]
    public void Base16_GetEncodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base16.GetEncodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetEncodedLength(int, Base32Variant, BaseFormattingOptions)" /> throws
    /// <see cref="OverflowException" /> when the result would overflow.
    /// </summary>
    [TestMethod]
    public void Base32_GetEncodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base32.GetEncodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetMaxDecodedLength(int)" /> throws on overflow.
    /// </summary>
    [TestMethod]
    public void Base32_GetMaxDecodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base32.GetMaxDecodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxDecodedLength(int)" /> throws on overflow.
    /// </summary>
    [TestMethod]
    public void Base58_GetMaxDecodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base58.GetMaxDecodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxEncodedLength(int)" /> throws on overflow. The Base58 expansion factor
    /// (138/100) means overflow kicks in earlier than the other encodings.
    /// </summary>
    [TestMethod]
    public void Base58_GetMaxEncodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base58.GetMaxEncodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetEncodedLength(int, Base64Variant, BaseFormattingOptions)" /> throws on
    /// overflow.
    /// </summary>
    [TestMethod]
    public void Base64_GetEncodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base64.GetEncodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.GetMaxDecodedLength(int)" /> throws on overflow.
    /// </summary>
    [TestMethod]
    public void Base64_GetMaxDecodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base64.GetMaxDecodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetMaxDecodedLength(int)" /> throws on overflow.
    /// </summary>
    [TestMethod]
    public void Base85_GetMaxDecodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base85.GetMaxDecodedLength(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetMaxEncodedLength(int, Base85Variant)" /> throws on overflow.
    /// </summary>
    [TestMethod]
    public void Base85_GetMaxEncodedLength_WhenOverflow_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Base85.GetMaxEncodedLength(int.MaxValue);
        });
    }

}
