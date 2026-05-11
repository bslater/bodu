// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that assigning a <see cref="Skein{T}.Key" /> of exactly
    /// <see cref="Skein{T}.MaxKeySizeBytes" /> bytes succeeds (boundary value test).
    /// </summary>
    [TestMethod]
    public void Key_WhenAssignedExactlyMaxKeySize_ShouldNotThrow()
    {
        using var skein = new TAlgorithm();

        try
        {
            skein.Key = new byte[Skein<TAlgorithm>.MaxKeySizeBytes];
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Assigning a {Skein<TAlgorithm>.MaxKeySizeBytes}-byte key should be the inclusive upper bound, " +
                $"but threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that switching <see cref="Skein{T}.Key" /> from a non-empty value back to an empty
    /// array causes the next <see cref="HashAlgorithm.ComputeHash(byte[])" /> call to produce the
    /// canonical plain-hash digest — the empty array is the documented sentinel for the unkeyed
    /// profile and must not leave the algorithm in a stale Skein-MAC state.
    /// </summary>
    [TestMethod]
    public void Key_WhenReassignedFromMacToEmpty_ShouldRestorePlainHashOutput()
    {
        byte[] input = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

        byte[] plainHash;
        using (var plain = new TAlgorithm())
            plainHash = plain.ComputeHash(input);

        using var skein = new TAlgorithm { Key = SkeinTestKey };
        _ = skein.ComputeHash(input);

        skein.Key = Array.Empty<byte>();
        byte[] reverted = skein.ComputeHash(input);

        CollectionAssert.AreEqual(plainHash, reverted,
            "Reassigning Key to an empty array must restore the canonical plain-hash digest.");
    }
}
