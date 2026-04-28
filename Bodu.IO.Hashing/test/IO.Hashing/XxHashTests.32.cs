// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHashTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable CS0618 // Type or member is obsolete — testing deprecated XxHash32 type

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="XxHash32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class XxHash32Tests
    : XxHashTests<XxHash32Tests, XxHash32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            // Seed = 0. Verified against the xxHash reference implementation.
            Empty = "02CC5D05",
            Abc = "80712ED5",
            QuickBrownFox = "E85EA4DE",
            Zeros16 = "8E022B3A",
            Sequential0To255 = "B4D58730",
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

        XxHash32 defaultSeed = new();
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        XxHash32 customSeed = new(0x9747B28C);
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
        XxHash32 sut = new(0x12345678u);
        Assert.AreEqual(0x12345678u, sut.Seed);
    }
}
