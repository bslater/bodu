// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein512" /> hash algorithm across every supported
/// (output size, operating mode) combination — the six <see cref="Skein512TestVariant" /> hash variants and the
/// matching Skein-MAC-512 variants — driven from the Skein 1.3 / NIST CD known-answer test vectors and the
/// Appendix B initial chaining values.
/// </summary>
[TestClass]
public partial class Skein512Tests
    : Security.Cryptography.SkeinTests<Skein512Tests, Skein512, Skein512TestVariant>
{
    private const string Skein512_512_EmptyHash =
        "BC5B4C50925519C290CC634277AE3D6257212395CBA733BBAD37A4AF0FA06AF4" +
        "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBA7A";

    /// <inheritdoc />
    protected override Skein512TestVariant DefaultVariant => Skein512TestVariant.Hash_512;

    /// <inheritdoc />
    public override IEnumerable<Skein512TestVariant> GetHashAlgorithmVariants() =>
        Enum.GetValues<Skein512TestVariant>();

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Skein512TestVariant variant) =>
        new KeyedAlgorithmSpecification
        {
            HashSize = OutputBitsFor(variant),
            HashBlockSize = 64,
            IsStateless = false,
            LongInputLength = 256,
            BoundaryLengths = [1, 16, 64, 128, 256],
            MinKeyLength = 16,
            MaxKeyLength = Skein<Skein512>.MaxKeySize / 8,
            ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein512>.MaxKeySize / 8],
            TestKey = SkeinTestKey,
            KnownAnswers = new HashAlgorithmKnownAnswers
            {
                Empty = variant == Skein512TestVariant.Hash_512 ? Skein512_512_EmptyHash : null,
            },
            KeyedAnswers = new KeyedHashAlgorithmKnownAnswers
            {
                Vectors = Skein512KnownAnswers.For(variant),
            },
        };

    /// <inheritdoc />
    protected override Skein512 CreateAlgorithm(Skein512TestVariant variant)
    {
        var algorithm = new Skein512(OutputBitsFor(variant));
        if (IsMacVariant(variant))
            algorithm.Key = SkeinTestKey;
        return algorithm;
    }

    /// <inheritdoc />
    /// <remarks>
    /// See <see cref="Skein256Tests.GetExpectedHashesForIncrementalInput" /> — the Skein 1.3 KAT files do not
    /// publish a dense byte-by-byte sequence; the data-driven keyed KAT path supplies the reference coverage.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Skein512TestVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyList<ulong>? GetExpectedAppendixBChainingValue(Skein512TestVariant variant) =>
        variant switch
        {
            Skein512TestVariant.Hash_128 => Skein512AppendixBIVs.Skein512_128,
            Skein512TestVariant.Hash_160 => Skein512AppendixBIVs.Skein512_160,
            Skein512TestVariant.Hash_224 => Skein512AppendixBIVs.Skein512_224,
            Skein512TestVariant.Hash_256 => Skein512AppendixBIVs.Skein512_256,
            Skein512TestVariant.Hash_384 => Skein512AppendixBIVs.Skein512_384,
            Skein512TestVariant.Hash_512 => Skein512AppendixBIVs.Skein512_512,
            _ => null,
        };

    /// <summary>
    /// Translates a <see cref="Skein512TestVariant" /> into the digest size in bits embedded in the variant name.
    /// </summary>
    private static int OutputBitsFor(Skein512TestVariant variant) =>
        variant switch
        {
            Skein512TestVariant.Hash_128 or Skein512TestVariant.Mac_128 => 128,
            Skein512TestVariant.Hash_160 or Skein512TestVariant.Mac_160 => 160,
            Skein512TestVariant.Hash_224 or Skein512TestVariant.Mac_224 => 224,
            Skein512TestVariant.Hash_256 or Skein512TestVariant.Mac_256 => 256,
            Skein512TestVariant.Hash_384 or Skein512TestVariant.Mac_384 => 384,
            Skein512TestVariant.Hash_512 or Skein512TestVariant.Mac_512 => 512,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Skein512 variant."),
        };
}
