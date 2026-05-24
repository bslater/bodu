// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.128.Append.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class MurmurHash3_128Tests
{

    /// <summary>
    /// Verifies that a non-zero seed produces a different hash than seed zero for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WithNonZeroSeed_ShouldProduceDifferentHashThanSeedZero()
    {
        var input = System.Text.Encoding.ASCII.GetBytes("test");

        MurmurHash3_128 defaultSeed = new();
        defaultSeed.Append(input);
        var hash0 = defaultSeed.GetCurrentHash();

        MurmurHash3_128 customSeed = new(0xDEADBEEF);
        customSeed.Append(input);
        var hash1 = customSeed.GetCurrentHash();

        CollectionAssert.AreNotEqual(hash0, hash1,
            "Non-zero seed must produce a different hash for identical input.");
    }

}
