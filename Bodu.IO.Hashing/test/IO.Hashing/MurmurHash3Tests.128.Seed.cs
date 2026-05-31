// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.128.Seed.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class MurmurHash3_128Tests
{

    /// <summary>
    /// Verifies that the seed property returns the value supplied at construction time.
    /// </summary>
    [TestMethod]
    public void Seed_AfterConstruction_ShouldReturnSuppliedValue()
    {
        MurmurHash3_128 sut = new(0xABCDEF01u);
        Assert.AreEqual(0xABCDEF01u, sut.Seed);
    }

}
