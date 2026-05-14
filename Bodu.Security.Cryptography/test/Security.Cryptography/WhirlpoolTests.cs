// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Whirlpool" /> hash algorithm covering all three published
/// revisions exposed via <see cref="WhirlpoolVersion" />.
/// </summary>
[TestClass]
public partial class WhirlpoolTests
    : Security.Cryptography.BlockHashAlgorithmTests<WhirlpoolTests, Whirlpool, WhirlpoolVersion>
{
    private static readonly HashAlgorithmSpecification BaseWhirlpoolSpecification = new()
    {
        HashBlockSize = 64,
        HashSize = 512,
        LongInputLength = 256,
        BoundaryLengths = [1, 64, 128, 192],
        MinNonZeroBytesForLongInput = 56,
    };

    /// <summary>The empty ISO test corpus payload.</summary>
    private static readonly byte[] IsoEmptyInput = Array.Empty<byte>();

    /// <summary>The ASCII byte <c>"a"</c> from the ISO test corpus.</summary>
    private static readonly byte[] IsoAInput = Encoding.ASCII.GetBytes("a");

    /// <summary>The ASCII bytes <c>"abc"</c> from the ISO test corpus.</summary>
    private static readonly byte[] IsoAbcInput = Encoding.ASCII.GetBytes("abc");

    /// <summary>The ISO test corpus <c>"message digest"</c> input.</summary>
    private static readonly byte[] IsoMessageDigestInput = Encoding.ASCII.GetBytes("message digest");

    /// <summary>The ISO test corpus lowercase alphabet input.</summary>
    private static readonly byte[] IsoLowercaseAlphabetInput = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz");

    /// <summary>The ISO test corpus mixed-case alphabet plus digits input.</summary>
    private static readonly byte[] IsoMixedAlphabetDigitsInput =
        Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");

    /// <summary>The ISO test corpus 88-byte repeated-digit input.</summary>
    private static readonly byte[] IsoEightDigitsRepeatedInput =
        Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678");

    /// <inheritdoc />
    public override IEnumerable<WhirlpoolVersion> GetHashAlgorithmVariants() => new[]
    {
        WhirlpoolVersion.WhirlpoolInfo3,
        WhirlpoolVersion.WhirlpoolInfo1,
        WhirlpoolVersion.WhirlpoolInfo2,
    };

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(WhirlpoolVersion variant) =>
        BaseWhirlpoolSpecification with
        {
            KnownAnswers = variant switch
            {
                WhirlpoolVersion.WhirlpoolInfo1 => WhirlpoolInfo1KnownAnswers,
                WhirlpoolVersion.WhirlpoolInfo2 => WhirlpoolInfo2KnownAnswers,
                WhirlpoolVersion.WhirlpoolInfo3 => WhirlpoolInfo3KnownAnswers,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
            },
        };

    /// <inheritdoc />
    protected override Whirlpool CreateAlgorithm() => new Whirlpool();

    /// <inheritdoc />
    protected override Whirlpool CreateAlgorithm(WhirlpoolVersion variant) =>
        new Whirlpool { Version = variant };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(WhirlpoolVersion variant) =>
        variant switch
        {
            WhirlpoolVersion.WhirlpoolInfo1 => WhirlpoolInfo1IncrementalHashes,
            WhirlpoolVersion.WhirlpoolInfo2 => WhirlpoolInfo2IncrementalHashes,
            WhirlpoolVersion.WhirlpoolInfo3 => WhirlpoolInfo3IncrementalHashes,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
}
