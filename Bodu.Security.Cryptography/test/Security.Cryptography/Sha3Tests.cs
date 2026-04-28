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
            // Known-answer test vectors from NIST FIPS 202, Appendix A (empty string) and the NIST
            // SHA-3 reference implementation (non-empty inputs).
            Sha3Variant.SHA3_224 => new HashAlgorithmSpecification
            {
                HashSize = 224,
                InputBlockSize = 144,
                OutputBlockSize = 28,
                LongInputLength = 288,
                BoundaryLengths = [1, 143, 144, 145, 288],
                MinNonZeroBytesForLongInput = 26,
                KnownAnswers = new()
                {
                    Empty = "6B4E03423667DBB73B6E15454F0EB1ABD4597F9A1B078E3F5B5A6BC7",
                    Abc = "51E6DB7CBA212F1490B290E44C588E3A028C8334055C877910C3EBE6",
                    Zeros16 = "A85C9DA5AB3F0A9AC1404C01306064FDA7665220EEBF2548A4CA542B",
                    Sequential0To255 = "D95C168E8F666375A1C7D574686D36293FCFD3717D79B212D47D97B7",
                },
            },
            Sha3Variant.SHA3_256 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                InputBlockSize = 136,
                OutputBlockSize = 32,
                LongInputLength = 272,
                BoundaryLengths = [1, 135, 136, 137, 272],
                MinNonZeroBytesForLongInput = 30,
                KnownAnswers = new()
                {
                    Empty = "A7FFC6F8BF1ED76651C14756A061D662F580FF4DE43B49FA82D80A4B80F8434A",
                    Abc = "7FB50120D9D1BC7504B4B7F1888D42ED98C0B47AB60A20BD4A2DA7B2C1360EFA",
                    Zeros16 = "61664696888A110278FF672620C85217E69AA662A83304052F1014D395F545BF",
                    Sequential0To255 = "CEB94E2E8BD45BBB4AF2A3AAA05CC3F7BC010A6C68E242923CE3731A108DF8E1",
                },
            },
            Sha3Variant.SHA3_384 => new HashAlgorithmSpecification
            {
                HashSize = 384,
                InputBlockSize = 104,
                OutputBlockSize = 48,
                LongInputLength = 208,
                BoundaryLengths = [1, 103, 104, 105, 208],
                MinNonZeroBytesForLongInput = 44,
                KnownAnswers = new()
                {
                    Empty = "0C63A75B845E4F7D01107D852E4C2485C51A50AAAA94FC61995E71BBEE983A2AC3713831264ADB47FB6BD1E058D5F004",
                    Abc = "38078331BAAA86DBE9B38224A0780E9661DAA35B42066A804EFD5215B2487B9728A19AE4940DDBCBDA39B697F13EBEBB",
                    Zeros16 = "A78E349C372B6ED02BCB0D141600CC2DB2308E2EA29F71DC0886C89E614B2B92CAE4FB75D1E60DE756DC4437EF70D427",
                    Sequential0To255 = "F5CC4DE5026A9359382B096635EA02874262DC3E657FD8EB10E297DF8A77326EF8F73220F4564AB23C092F24E68FDA76",
                },
            },
            Sha3Variant.SHA3_512 => new HashAlgorithmSpecification
            {
                HashSize = 512,
                InputBlockSize = 72,
                OutputBlockSize = 64,
                LongInputLength = 144,
                BoundaryLengths = [1, 71, 72, 73, 144],
                MinNonZeroBytesForLongInput = 56,
                KnownAnswers = new()
                {
                    Empty = "A69F73CCA23A9AC5C8B567DC185A756E97C982164FE25859E0D1DCC1475C80A615B2123AF1F5F94C11E3E9402C3AC558F500199D95B6D3E301758586281DCD26",
                    Abc = "077AA33882B1AAF06DA41C7ED3B6A40D7128DEE23505CA2689C47637111C4701645FABC5EE1B9DCD039231D2D086BFF9819CE2DA8647432A73966494DD1A77AD",
                    Zeros16 = "F0140E314EE38D4472393680E7A72A81ABB36B134B467D90EA943B7AA1EA03BF2323BC1A2DF91F7230A225952E162F6629CF435E53404E9CDD727A2D94E4F909",
                    Sequential0To255 = "800D31CEC315A30CF647DF7736BC2A57DF5B82CAE0FCE83EDFED4B5F3C34A44DFEBE79D35D89439D8208D26B69A0A9D050F7D3966A03D77A7B1111772DBB9B69",
                },
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
