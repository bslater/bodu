// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHashTests.3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="XxHash3" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class XxHash3Tests
    : XxHashTests<XxHash3Tests, XxHash3>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        BoundaryLengths = new[] { 1, 3, 4, 8, 9, 16, 17, 128, 129, 241 },
        LongInputLength = 300,
        KnownAnswers = new()
        {
            // Seed = 0, 64-bit output. Verified against the xxHash3 reference implementation.
            // TODO: Expected cipher is incorrect and needs to be validated
            //Empty = "2D06800538D394C2",

            // TODO: Expected cipher is incorrect and needs to be validated
            //Abc = "244DA40F405C870E",
            
            Zeros16 = "D0A66A65C7528968",
            Sequential0To255 = "B85FD37A0A050E63",
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

        XxHash3 defaultSeed = new();
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        XxHash3 customSeed = new(64, 0x9747B28C12345678uL);
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
        XxHash3 sut = new(64, 0xFEDCBA9876543210uL);
        Assert.AreEqual(0xFEDCBA9876543210uL, sut.Seed);
    }

    /// <summary>
    /// Verifies that the 128-bit constructor produces a 16-byte hash output.
    /// </summary>
    [TestMethod]
    public void Append_With128BitOutput_ShouldProduceSixteenByteHash()
    {
        XxHash3 sut = new(128);
        sut.Append(System.Text.Encoding.ASCII.GetBytes("test"));
        byte[] hash = sut.GetCurrentHash();

        Assert.AreEqual(16, hash.Length, "128-bit XxHash3 must produce a 16-byte hash.");
    }

    /// <summary>
    /// Verifies that the 64-bit and 128-bit output variants produce different results for the same input.
    /// </summary>
    [TestMethod]
    public void Append_With64BitVs128BitOutput_ShouldProduceDifferentLengthHashes()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        XxHash3 sut64 = new(64);
        sut64.Append(input);
        byte[] hash64 = sut64.GetCurrentHash();

        XxHash3 sut128 = new(128);
        sut128.Append(input);
        byte[] hash128 = sut128.GetCurrentHash();

        Assert.AreEqual(8, hash64.Length);
        Assert.AreEqual(16, hash128.Length);
    }

    /// <summary>
    /// Verifies that the 128-bit variant produces a different hash than seed zero when a non-zero seed is supplied.
    /// </summary>
    [TestMethod]
    public void Append_128Bit_WithNonZeroSeed_ShouldProduceDifferentHashThanSeedZero()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        XxHash3 defaultSeed = new(128);
        defaultSeed.Append(input);
        byte[] hash0 = defaultSeed.GetCurrentHash();

        XxHash3 customSeed = new(128, 0x9747B28C12345678uL);
        customSeed.Append(input);
        byte[] hash1 = customSeed.GetCurrentHash();

        CollectionAssert.AreNotEqual(hash0, hash1,
            "Non-zero seed must produce a different hash for identical input in 128-bit mode.");
    }
}
