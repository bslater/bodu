// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod11_2Tests.InvalidChar.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public sealed partial class Iso7064Mod11_2Tests
{
    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.Compute(ReadOnlySpan{char})" /> rejects a body containing any
    /// character outside the ASCII decimal-digit range with <see cref="ArgumentOutOfRangeException" /> whose
    /// <c>ParamName</c> is the body argument.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsNonDigit_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Iso7064Mod11_2.Compute("12a4".AsSpan());
        });
        Assert.AreEqual("digits", ex.ParamName);
    }
}
