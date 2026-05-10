// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BKDRTests
{
    /// <summary>
    /// Verifies that constructing with an unsupported seed multiplier throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSeedIsUnsupported_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BKDR(1234U));
    }
}
