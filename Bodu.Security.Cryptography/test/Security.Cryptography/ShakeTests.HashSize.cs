// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.HashSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that assigning <see cref="Shake.HashSize" /> a value that is not a positive multiple of 8 throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>value</c> parameter, as the property documents and as
    /// the constructor does for the same output size.
    /// </summary>
    /// <param name="hashSize">The invalid output size, in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-8)]
    [DataRow(12)]
    public void HashSize_WhenSetToValueThatIsNotPositiveMultipleOfEight_ShouldThrowArgumentOutOfRangeException(int hashSize)
    {
        using var algorithm = new Shake();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.HashSize = hashSize;
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
