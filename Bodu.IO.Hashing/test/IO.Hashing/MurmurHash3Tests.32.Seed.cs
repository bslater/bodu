// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.32.Seed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class MurmurHash3_32Tests
{

    /// <summary>
    /// Verifies that the seed property returns the value supplied at construction time.
    /// </summary>
    [TestMethod]
    public void Seed_AfterConstruction_ShouldReturnSuppliedValue()
    {
        MurmurHash3_32 sut = new(0x12345678u);
        Assert.AreEqual(0x12345678u, sut.Seed);
    }

}
