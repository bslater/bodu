// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumberTests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public sealed partial class AbaRoutingNumberTests 
{
    /// <summary>
    /// Verifies that <see cref="AbaRoutingNumber.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length
    /// is not exactly <see cref="AbaRoutingNumber.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(AbaRoutingNumber.IsValid("0110000150".AsSpan()));
        Assert.IsFalse(AbaRoutingNumber.IsValid("01100001".AsSpan()));
    }

    /// <summary>
    /// Verifies that a real-world Federal Reserve Bank routing number round-trips through
    /// <see cref="AbaRoutingNumber.IsValid(ReadOnlySpan{char})" />.
    /// </summary>
    [TestMethod]
    public void IsValid_FrbBostonRoutingNumber_ShouldReturnTrue()
    {
        Assert.IsTrue(AbaRoutingNumber.IsValid("011000015".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="AbaRoutingNumber.IsValid(ReadOnlySpan{char})" /> returns <see langword="false" />
    /// when a sequence of the correct length contains a non-digit character — exercising the per-character
    /// validation branch of the underlying weighted-mod-10 routine without throwing.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceContainsNonDigit_ShouldReturnFalse()
    {
        // Length is correct (9) so the routine reaches the per-character validation; the 'A' at position 8
        // forces the early-return path that reports the sequence as invalid.
        Assert.IsFalse(AbaRoutingNumber.IsValid("01100001A".AsSpan()));
    }
}
