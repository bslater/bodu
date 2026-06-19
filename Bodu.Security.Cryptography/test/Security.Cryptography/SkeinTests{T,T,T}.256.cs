// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests{T,T,T}.256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein256" /> hash algorithm across every supported
/// (output size, operating mode) combination — the four <see cref="Skein256TestVariant" /> hash variants and the
/// matching Skein-MAC-256 variants — driven from the Skein 1.3 / NIST CD known-answer test vectors and the
/// Appendix B initial chaining values.
/// </summary>
[TestClass]
public partial class Skein256Tests
    : Security.Cryptography.SkeinTests<Skein256Tests, Skein256, Skein256TestVariant>
{
    private const string Skein256_256_EmptyHash =
        "C8877087DA56E072870DAA843F176E9453115929094C3A40C463A196C29BF7BA";

    /// <inheritdoc />
    protected override Skein256TestVariant DefaultVariant => Skein256TestVariant.Hash_256;

    /// <inheritdoc />
    public override IEnumerable<Skein256TestVariant> GetHashAlgorithmVariants() =>
        Enum.GetValues<Skein256TestVariant>();

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Skein256TestVariant variant) =>
        new KeyedAlgorithmSpecification
        {
            HashSize = OutputBitsFor(variant),
            HashBlockSize = 32,
            IsStateless = false,
            LongInputLength = 200,
            BoundaryLengths = [1, 8, 16, 32, 64, 128],
            MinKeyLength = 16,
            MaxKeyLength = Skein<Skein256>.MaxKeySize / 8,
            ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein256>.MaxKeySize / 8],
            TestKey = SkeinTestKey,
            KnownAnswers = new HashAlgorithmKnownAnswers
            {
                Empty = variant == Skein256TestVariant.Hash_256 ? Skein256_256_EmptyHash : null,
            },
            KeyedAnswers = new KeyedHashAlgorithmKnownAnswers
            {
                Vectors = Skein256KnownAnswers.For(variant),
            },
        };

    /// <inheritdoc />
    protected override Skein256 CreateAlgorithm(Skein256TestVariant variant)
    {
        var algorithm = new Skein256(OutputBitsFor(variant));
        if (IsMacVariant(variant))
            algorithm.Key = SkeinTestKey;
        return algorithm;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The framework iterates this set to exercise every incremental input length. The Skein 1.3 KAT files do not
    /// publish a dense byte-by-byte sequence, so this returns an empty list and the incremental test reports
    /// inconclusive. The Skein KAT coverage instead arrives via
    /// <see cref="SkeinTests{TTest, TAlgorithm, TVariant}.ComputeHash_WhenUsingKeyedKnownAnswer_ShouldMatchExpected" />.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Skein256TestVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyList<ulong>? GetExpectedAppendixBChainingValue(Skein256TestVariant variant) =>
        variant switch
        {
            Skein256TestVariant.Hash_128 => Skein256AppendixBIVs.Skein256_128,
            Skein256TestVariant.Hash_160 => Skein256AppendixBIVs.Skein256_160,
            Skein256TestVariant.Hash_224 => Skein256AppendixBIVs.Skein256_224,
            Skein256TestVariant.Hash_256 => Skein256AppendixBIVs.Skein256_256,
            _ => null,
        };

    /// <summary>
    /// Translates a <see cref="Skein256TestVariant" /> into the digest size in bits embedded in the variant name.
    /// </summary>
    private static int OutputBitsFor(Skein256TestVariant variant) =>
        variant switch
        {
            Skein256TestVariant.Hash_128 or Skein256TestVariant.Mac_128 => 128,
            Skein256TestVariant.Hash_160 or Skein256TestVariant.Mac_160 => 160,
            Skein256TestVariant.Hash_224 or Skein256TestVariant.Mac_224 => 224,
            Skein256TestVariant.Hash_256 or Skein256TestVariant.Mac_256 => 256,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Skein256 variant."),
        };
}
