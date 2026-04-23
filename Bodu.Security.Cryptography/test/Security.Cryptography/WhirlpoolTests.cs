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
        InputBlockSize = 64,
        OutputBlockSize = 64,
        HashSize = 512,
        LongInputLength = 256,
        BoundaryLengths = [1, 64, 128, 192],
        MinNonZeroBytesForLongInput = 56,
    };

    /// <inheritdoc />
    public override IEnumerable<WhirlpoolVersion> GetHashAlgorithmVariants() => new[]
    {
        WhirlpoolVersion.WhirlpoolInfo3,
        WhirlpoolVersion.WhirlpoolInfo1,
        WhirlpoolVersion.WhirlpoolInfo2,
    };

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(WhirlpoolVersion variant) =>
        BaseWhirlpoolSpecification;

    /// <inheritdoc />
    protected override Whirlpool CreateAlgorithm() => new Whirlpool();

    /// <inheritdoc />
    protected override Whirlpool CreateAlgorithm(WhirlpoolVersion variant) =>
        new Whirlpool { Version = variant };

    private static readonly IReadOnlyDictionary<string, byte[]> CustomInputs = new Dictionary<string, byte[]>
    {
        ["IsoEmpty"] = Array.Empty<byte>(),
        ["IsoA"] = Encoding.ASCII.GetBytes("a"),
        ["IsoAbc"] = Encoding.ASCII.GetBytes("abc"),
        ["IsoMessageDigest"] = Encoding.ASCII.GetBytes("message digest"),
        ["IsoLowercaseAlphabet"] = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"),
        ["IsoMixedAlphabetDigits"] = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"),
        ["IsoEightDigitsRepeated"] = Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678"),
    };

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetTestVectors(WhirlpoolVersion variant)
    {
        foreach (var vector in base.GetTestVectors(variant))
            yield return vector;

        var expected = GetExpectedHashesForNamedInputs(variant);
        foreach (var (name, input) in CustomInputs)
        {
            if (expected.TryGetValue(name, out var hex))
            {
                yield return new KnownAnswerTest
                {
                    Name = name,
                    Input = input,
                    ExpectedOutput = Convert.FromHexString(hex)
                };
            }
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(WhirlpoolVersion variant) =>
        variant switch
        {
            WhirlpoolVersion.WhirlpoolInfo1 => WhirlpoolInfo1IncrementalHashes,
            WhirlpoolVersion.WhirlpoolInfo2 => WhirlpoolInfo2IncrementalHashes,
            WhirlpoolVersion.WhirlpoolInfo3 => WhirlpoolInfo3IncrementalHashes,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(WhirlpoolVersion variant) =>
        variant switch
        {
            WhirlpoolVersion.WhirlpoolInfo1 => WhirlpoolInfo1NamedHashes,
            WhirlpoolVersion.WhirlpoolInfo2 => WhirlpoolInfo2NamedHashes,
            WhirlpoolVersion.WhirlpoolInfo3 => WhirlpoolInfo3NamedHashes,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
}
