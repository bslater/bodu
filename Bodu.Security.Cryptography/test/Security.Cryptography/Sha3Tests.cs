// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Sha3Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Sha3" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Sha3Tests
    : HashAlgorithmTests<Sha3Tests, Sha3, Sha3Tests.Sha3Variant>
{
    /// <summary>Identifies the output-size variants of <see cref="Sha3" />.</summary>
    public enum Sha3Variant
    {
        /// <summary>SHA3-224 (224-bit output, rate = 144 bytes).</summary>
        SHA3_224,

        /// <summary>SHA3-256 (256-bit output, rate = 136 bytes).</summary>
        SHA3_256,

        /// <summary>SHA3-384 (384-bit output, rate = 104 bytes).</summary>
        SHA3_384,

        /// <summary>SHA3-512 (512-bit output, rate = 72 bytes).</summary>
        SHA3_512,
    }

    /// <inheritdoc />
    protected override Sha3Variant DefaultVariant => Sha3Variant.SHA3_256;

    /// <inheritdoc />
    public override IEnumerable<Sha3Variant> GetHashAlgorithmVariants() =>
    [
        Sha3Variant.SHA3_224,
        Sha3Variant.SHA3_256,
        Sha3Variant.SHA3_384,
        Sha3Variant.SHA3_512,
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Sha3Variant variant) =>
        variant switch
        {
            Sha3Variant.SHA3_224 => new HashAlgorithmSpecification
            {
                HashSize = 224,
                InputBlockSize = 144,
                OutputBlockSize = 28,
                LongInputLength = 288,
                BoundaryLengths = [1, 143, 144, 145, 288],
                MinNonZeroBytesForLongInput = 26,
            },
            Sha3Variant.SHA3_256 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                InputBlockSize = 136,
                OutputBlockSize = 32,
                LongInputLength = 272,
                BoundaryLengths = [1, 135, 136, 137, 272],
                MinNonZeroBytesForLongInput = 30,
            },
            Sha3Variant.SHA3_384 => new HashAlgorithmSpecification
            {
                HashSize = 384,
                InputBlockSize = 104,
                OutputBlockSize = 48,
                LongInputLength = 208,
                BoundaryLengths = [1, 103, 104, 105, 208],
                MinNonZeroBytesForLongInput = 44,
            },
            Sha3Variant.SHA3_512 => new HashAlgorithmSpecification
            {
                HashSize = 512,
                InputBlockSize = 72,
                OutputBlockSize = 64,
                LongInputLength = 144,
                BoundaryLengths = [1, 71, 72, 73, 144],
                MinNonZeroBytesForLongInput = 56,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Sha3 CreateAlgorithm(Sha3Variant variant) =>
        variant switch
        {
            Sha3Variant.SHA3_224 => new Sha3(224),
            Sha3Variant.SHA3_256 => new Sha3(256),
            Sha3Variant.SHA3_384 => new Sha3(384),
            Sha3Variant.SHA3_512 => new Sha3(512),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Sha3Variant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Sha3Variant variant) =>
        variant switch
        {
            // Known-answer test vectors from NIST FIPS 202, Appendix A.
            Sha3Variant.SHA3_224 => new Dictionary<string, string>
            {
                ["Empty"] = "6B4E03423667DBB73B6E15454F0EB1ABD4597F9A1B078E3F5B5A6BC7",
            },
            Sha3Variant.SHA3_256 => new Dictionary<string, string>
            {
                ["Empty"] = "A7FFC6F8BF1ED76651C14756A061D662F580FF4DE43B49FA82D80A4B80F8434A",
            },
            Sha3Variant.SHA3_384 => new Dictionary<string, string>
            {
                ["Empty"] = "0C63A75B845E4F7D01107D852E4C2485C51A50AAAA94FC61995E71BBEE983A2AC3713831264ADB47FB6BD1E058D5F004",
            },
            Sha3Variant.SHA3_512 => new Dictionary<string, string>
            {
                ["Empty"] = "A69F73CCA23A9AC5C8B567DC185A756E97C982164FE25859E0D1DCC1475C80A615B2123AF1F5F94C11E3E9402C3AC558F500199D95B6D3E301758586281DCD26",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
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
            _ = new Sha3(hashSize);
        });
    }

    /// <summary>
    /// Verifies that all four SHA-3 variants produce digests of different lengths for the same input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_AcrossAllVariants_ShouldReturnCorrectOutputLength()
    {
        foreach (Sha3Variant variant in GetHashAlgorithmVariants())
        {
            using Sha3 sut = CreateAlgorithm(variant);
            byte[] hash = sut.ComputeHash(Array.Empty<byte>());
            int expectedBytes = GetSpecification(variant).HashSize / 8;
            Assert.AreEqual(expectedBytes, hash.Length, $"Expected {expectedBytes} bytes for {variant}.");
        }
    }
}
