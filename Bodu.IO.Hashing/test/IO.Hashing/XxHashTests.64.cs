// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHashTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable CS0618 // Type or member is obsolete — testing deprecated XxHash64 type

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="XxHash64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class XxHash64Tests
    : XxHashTests<XxHash64Tests, XxHash64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        KnownAnswers = new()
        {
            // Seed = 0. Verified against the xxHash reference implementation.
            Empty = "EF46DB3751D8E999",
            Abc = "E66AE7354FCFEE98",
            Zeros16 = "AF09F71516247C32",
            Sequential0To255 = "0F7D97507CAAD693",
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

        XxHash64 defaultSeed = new();
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        XxHash64 customSeed = new(0x9747B28C12345678uL);
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
        XxHash64 sut = new(0xFEDCBA9876543210uL);
        Assert.AreEqual(0xFEDCBA9876543210uL, sut.Seed);
    }
}
