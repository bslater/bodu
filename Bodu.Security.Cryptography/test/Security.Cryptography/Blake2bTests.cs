// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Blake2b" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Blake2bTests
    : HashAlgorithmTests<Blake2bTests, Blake2b, Blake2bTests.Blake2bVariant>
{
    /// <summary>Identifies the output-size variants of <see cref="Blake2b" />.</summary>
    public enum Blake2bVariant
    {
        /// <summary>128-bit (16-byte) output.</summary>
        Blake2b_128,

        /// <summary>160-bit (20-byte) output.</summary>
        Blake2b_160,

        /// <summary>192-bit (24-byte) output.</summary>
        Blake2b_192,

        /// <summary>224-bit (28-byte) output.</summary>
        Blake2b_224,

        /// <summary>256-bit (32-byte) output.</summary>
        Blake2b_256,

        /// <summary>384-bit (48-byte) output.</summary>
        Blake2b_384,

        /// <summary>512-bit (64-byte) output.</summary>
        Blake2b_512,
    }

    private static readonly HashAlgorithmSpecification BaseSpecification = new()
    {
        InputBlockSize = 128,
        OutputBlockSize = 64,
        HashSize = 512,
        LongInputLength = 384,
        BoundaryLengths = [1, 127, 128, 129, 256],
        MinNonZeroBytesForLongInput = 56,
    };

    /// <inheritdoc />
    protected override Blake2bVariant DefaultVariant => Blake2bVariant.Blake2b_512;

    /// <inheritdoc />
    public override IEnumerable<Blake2bVariant> GetHashAlgorithmVariants() =>
    [
        Blake2bVariant.Blake2b_128,
        Blake2bVariant.Blake2b_160,
        Blake2bVariant.Blake2b_192,
        Blake2bVariant.Blake2b_224,
        Blake2bVariant.Blake2b_256,
        Blake2bVariant.Blake2b_384,
        Blake2bVariant.Blake2b_512,
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Blake2bVariant variant) =>
        variant switch
        {
            Blake2bVariant.Blake2b_128 => BaseSpecification with { HashSize = 128, OutputBlockSize = 16, MinNonZeroBytesForLongInput = 14 },
            Blake2bVariant.Blake2b_160 => BaseSpecification with { HashSize = 160, OutputBlockSize = 20, MinNonZeroBytesForLongInput = 18 },
            Blake2bVariant.Blake2b_192 => BaseSpecification with { HashSize = 192, OutputBlockSize = 24, MinNonZeroBytesForLongInput = 22 },
            Blake2bVariant.Blake2b_224 => BaseSpecification with { HashSize = 224, OutputBlockSize = 28, MinNonZeroBytesForLongInput = 26 },
            Blake2bVariant.Blake2b_256 => BaseSpecification with { HashSize = 256, OutputBlockSize = 32, MinNonZeroBytesForLongInput = 30 },
            Blake2bVariant.Blake2b_384 => BaseSpecification with { HashSize = 384, OutputBlockSize = 48, MinNonZeroBytesForLongInput = 44 },
            Blake2bVariant.Blake2b_512 => BaseSpecification,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Blake2b CreateAlgorithm(Blake2bVariant variant) =>
        variant switch
        {
            Blake2bVariant.Blake2b_128 => new Blake2b(128),
            Blake2bVariant.Blake2b_160 => new Blake2b(160),
            Blake2bVariant.Blake2b_192 => new Blake2b(192),
            Blake2bVariant.Blake2b_224 => new Blake2b(224),
            Blake2bVariant.Blake2b_256 => new Blake2b(256),
            Blake2bVariant.Blake2b_384 => new Blake2b(384),
            Blake2bVariant.Blake2b_512 => new Blake2b(512),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Blake2bVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Blake2bVariant variant) =>
        variant switch
        {
            // Known-answer test vectors from RFC 7693 and the BLAKE2 reference implementation.
            Blake2bVariant.Blake2b_256 => new Dictionary<string, string>
            {
                ["Empty"] = "0E5751C026E543B2E8AB2EB06099DAA1D1E5DF47778F7787FAAB45CDF12FE3A8",
            },
            Blake2bVariant.Blake2b_512 => new Dictionary<string, string>
            {
                ["Empty"] = "786A02F742015903C6C6FD852552D272912F4740E15847618A86E217F71F5419D25E1031AFEE585313896444934EB04B903A685B1448B755D56F701AFE9BE2CE",
            },
            _ => new Dictionary<string, string>(),
        };

    /// <summary>
    /// Verifies that requesting an unsupported hash size throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenHashSizeIsUnsupported_ShouldThrowArgumentOutOfRangeException()
    {
        int hashSize = 300;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Blake2b(hashSize);
        });
    }

    /// <summary>
    /// Verifies that all supported hash sizes produce a non-empty digest of the expected length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_ForAllSupportedSizes_ShouldReturnCorrectLength()
    {
        foreach (int size in Blake2b.ValidHashSizes)
        {
            using Blake2b sut = new(size);
            byte[] hash = sut.ComputeHash(Array.Empty<byte>());
            Assert.AreEqual(size / 8, hash.Length, $"Expected {size / 8} bytes for {size}-bit output.");
        }
    }
}
