// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.32.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        var input = System.Text.Encoding.ASCII.GetBytes("test");

        MurmurHash3_32 defaultSeed = new();
        defaultSeed.Append(input);
        var hash0 = defaultSeed.GetCurrentHash();

        MurmurHash3_32 customSeed = new(0xDEADBEEF);
        customSeed.Append(input);
        var hash1 = customSeed.GetCurrentHash();

        CollectionAssert.AreNotEqual(hash0, hash1,
            "Non-zero seed must produce a different hash for identical input.");
    }
}
