// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="MurmurHash3_32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class MurmurHash3_32Tests
    : MurmurHash3Tests<MurmurHash3_32Tests, MurmurHash3_32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            // Seed = 0. Verified against the reference MurmurHash3_x86_32 implementation.
            Empty = "00000000",
            Abc = "35C5C143",
            Zeros16 = "F8CD3481",
            Sequential0To255 = "00B63463",
        },
    };

    /// <inheritdoc />
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) =>
        Array.Empty<string>();

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
