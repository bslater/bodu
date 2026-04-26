// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Blake2s" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Blake2sTests
    : HashAlgorithmTests<Blake2sTests, Blake2s, Blake2sTests.Blake2sVariant>
{
    /// <summary>Identifies the output-size variants of <see cref="Blake2s" />.</summary>
    public enum Blake2sVariant
    {
        /// <summary>128-bit (16-byte) output.</summary>
        Blake2s_128,

        /// <summary>160-bit (20-byte) output.</summary>
        Blake2s_160,

        /// <summary>192-bit (24-byte) output.</summary>
        Blake2s_192,

        /// <summary>224-bit (28-byte) output.</summary>
        Blake2s_224,

        /// <summary>256-bit (32-byte) output.</summary>
        Blake2s_256,
    }

    private static readonly HashAlgorithmSpecification BaseSpecification = new()
    {
        InputBlockSize = 64,
        OutputBlockSize = 32,
        HashSize = 256,
        LongInputLength = 192,
        BoundaryLengths = [1, 63, 64, 65, 128],
        MinNonZeroBytesForLongInput = 28,
    };

    /// <inheritdoc />
    protected override Blake2sVariant DefaultVariant => Blake2sVariant.Blake2s_256;

    /// <inheritdoc />
    public override IEnumerable<Blake2sVariant> GetHashAlgorithmVariants() =>
    [
        Blake2sVariant.Blake2s_128,
        Blake2sVariant.Blake2s_160,
        Blake2sVariant.Blake2s_192,
        Blake2sVariant.Blake2s_224,
        Blake2sVariant.Blake2s_256,
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Blake2sVariant variant) =>
        variant switch
        {
            Blake2sVariant.Blake2s_128 => BaseSpecification with { HashSize = 128, OutputBlockSize = 16, MinNonZeroBytesForLongInput = 14 },
            Blake2sVariant.Blake2s_160 => BaseSpecification with { HashSize = 160, OutputBlockSize = 20, MinNonZeroBytesForLongInput = 18 },
            Blake2sVariant.Blake2s_192 => BaseSpecification with { HashSize = 192, OutputBlockSize = 24, MinNonZeroBytesForLongInput = 22 },
            Blake2sVariant.Blake2s_224 => BaseSpecification with { HashSize = 224, OutputBlockSize = 28, MinNonZeroBytesForLongInput = 26 },
            Blake2sVariant.Blake2s_256 => BaseSpecification,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Blake2s CreateAlgorithm(Blake2sVariant variant) =>
        variant switch
        {
            Blake2sVariant.Blake2s_128 => new Blake2s(128),
            Blake2sVariant.Blake2s_160 => new Blake2s(160),
            Blake2sVariant.Blake2s_192 => new Blake2s(192),
            Blake2sVariant.Blake2s_224 => new Blake2s(224),
            Blake2sVariant.Blake2s_256 => new Blake2s(256),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Blake2sVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Blake2sVariant variant) =>
        variant switch
        {
            // Known-answer test vector from RFC 7693 and the BLAKE2 reference implementation.
            Blake2sVariant.Blake2s_256 => new Dictionary<string, string>
            {
                ["Empty"] = "69217A3079908094E11121D042354A7C1F55B6482CA1A51E1B250DFD1ED0EEF9",
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
            _ = new Blake2s(hashSize);
        });
    }

    /// <summary>
    /// Verifies that all supported hash sizes produce a non-empty digest of the expected length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_ForAllSupportedSizes_ShouldReturnCorrectLength()
    {
        foreach (int size in Blake2s.ValidHashSizes)
        {
            using Blake2s sut = new(size);
            byte[] hash = sut.ComputeHash(Array.Empty<byte>());
            Assert.AreEqual(size / 8, hash.Length, $"Expected {size / 8} bytes for {size}-bit output.");
        }
    }
}
