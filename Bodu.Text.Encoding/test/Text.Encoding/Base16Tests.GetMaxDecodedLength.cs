// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.GetMaxDecodedLength.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.GetMaxDecodedLength" /> returns half of the character count (rounded down).
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenEvenCharCount_ShouldReturnHalf()
    {
        Assert.AreEqual(4, Base16.GetMaxDecodedLength(8));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetMaxDecodedLength" /> rejects a negative input.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenNegativeInput_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.GetMaxDecodedLength(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetMaxDecodedLength" /> rounds an odd input down (the trailing nibble cannot
    /// form a complete byte).
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenOddCharCount_ShouldRoundDown()
    {
        Assert.AreEqual(2, Base16.GetMaxDecodedLength(5));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetMaxDecodedLength" /> returns zero for an empty input.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenZero_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base16.GetMaxDecodedLength(0));
    }

}
