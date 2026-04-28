// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn10Tests.InvalidChar.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public sealed partial class Isbn10Tests
{
    /// <summary>
    /// Verifies that <see cref="Isbn10.Compute(ReadOnlySpan{char})" /> rejects a body containing any character
    /// outside the ASCII decimal-digit range with <see cref="ArgumentOutOfRangeException" /> whose
    /// <c>ParamName</c> is the body argument.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsNonDigit_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Isbn10.Compute("12a456789".AsSpan());
        });
        Assert.AreEqual("digits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Isbn10.Compute(ReadOnlySpan{char})" /> rejects the <c>'X'</c> sentinel inside a
    /// body — <c>X</c> is permitted only as a check character, never as a body digit.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsXSentinel_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Isbn10.Compute("X06406152".AsSpan());
        });
    }
}
