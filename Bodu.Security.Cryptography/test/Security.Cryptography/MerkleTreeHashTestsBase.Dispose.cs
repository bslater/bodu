// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

public abstract partial class MerkleTreeHashTestsBase<THasher>
    where THasher : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Dispose — shared basics
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        hasher.Dispose();
        hasher.Dispose();
    }

    /// <summary>
    /// Verifies that the hasher can be used inside a <c>using</c> statement without error and
    /// produces a valid result before cleanup.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenUsedWithUsingStatement_ShouldDisposeCleanly()
    {
        byte[] result;
        using (var hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut))
            result = ComputeHash(hasher, MakeData(4));

        Assert.IsNotNull(result);
    }
}
