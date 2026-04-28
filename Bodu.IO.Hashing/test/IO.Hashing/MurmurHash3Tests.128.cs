// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="MurmurHash3_128" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class MurmurHash3_128Tests
    : MurmurHash3Tests<MurmurHash3_128Tests, MurmurHash3_128>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 16,
        KnownAnswers = new()
        {
            // Seed = 0. Verified against the reference MurmurHash3_x64_128 implementation.
            Empty = "00000000000000000000000000000000",
            Abc = "DE2FF8A27764BE8D4AA65B1C4FEE2B9E",
            QuickBrownFox = "6C1B07BC7BBC4BE347939AC4A93C437A",
            Zeros16 = "D618A97DF21BBD4BB61C79CDECA965B4",
            Sequential0To255 = "9D586942829AB00383DAED0E6167FB85",
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

        MurmurHash3_128 defaultSeed = new();
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        MurmurHash3_128 customSeed = new(0xDEADBEEF);
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
        MurmurHash3_128 sut = new(0xABCDEF01u);
        Assert.AreEqual(0xABCDEF01u, sut.Seed);
    }
}
