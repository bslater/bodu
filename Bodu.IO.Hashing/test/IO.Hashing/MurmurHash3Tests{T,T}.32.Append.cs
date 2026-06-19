// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests{T,T}.32.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class MurmurHash3_32Tests
{

    /// <summary>
    /// Verifies that a non-zero seed produces a different hash than seed zero for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WithNonZeroSeed_ShouldProduceDifferentHashThanSeedZero()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        MurmurHash3_32 defaultSeed = new();
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        MurmurHash3_32 customSeed = new(0xDEADBEEF);
        customSeed.Append(input);
        byte[] hash1 = customSeed.GetCurrentHash();

        CollectionAssert.AreNotEqual(hash0, hash1,
            "Non-zero seed must produce a different hash for identical input.");
    }

}
