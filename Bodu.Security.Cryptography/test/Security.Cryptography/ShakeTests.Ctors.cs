// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that requesting an invalid security level throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctors_WhenSecurityLevelIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Shake(256, 192);
        });
    }

    /// <summary>
    /// Verifies that requesting a non-positive output size throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctors_WhenOutputBitsIsNotPositiveMultipleOf8_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Shake(0, 128);
        });
    }
}
