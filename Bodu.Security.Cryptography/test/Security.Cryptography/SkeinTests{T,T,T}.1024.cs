// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests{T,T,T}.1024.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein1024" /> hash algorithm across every supported
/// (output size, operating mode) combination — the three <see cref="Skein1024TestVariant" /> hash variants and the
/// matching Skein-MAC-1024 variants — driven from the Skein 1.3 / NIST CD known-answer test vectors and the
/// Appendix B initial chaining values.
/// </summary>
[TestClass]
public partial class Skein1024Tests
    : Security.Cryptography.SkeinTests<Skein1024Tests, Skein1024, Skein1024TestVariant>
{
    private const string Skein1024_1024_EmptyHash =
        "0FFF9563BB3279289227AC77D319B6FFF8D7E9F09DA1247B72A0A265CD6D2A62" +
        "645AD547ED8193DB48CFF847C06494A03F55666D3B47EB4C20456C9373C86297" +
        "D630D5578EBD34CB40991578F9F52B18003EFA35D3DA6553FF35DB91B81AB890" +
        "BEC1B189B7F52CB2A783EBB7D823D725B0B4A71F6824E88F68F982EEFC6D19C6";

    /// <inheritdoc />
    protected override Skein1024TestVariant DefaultVariant => Skein1024TestVariant.Hash_1024;

    /// <inheritdoc />
    public override IEnumerable<Skein1024TestVariant> GetHashAlgorithmVariants() =>
        Enum.GetValues<Skein1024TestVariant>();

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Skein1024TestVariant variant) =>
        new KeyedAlgorithmSpecification
        {
            HashSize = OutputBitsFor(variant),
            IsStateless = false,
            HashBlockSize = 128,
            LongInputLength = 512,
            BoundaryLengths = [1, 16, 128, 256, 512],
            MinKeyLength = 16,
            MaxKeyLength = Skein<Skein1024>.MaxKeySize / 8,
            ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein1024>.MaxKeySize / 8],
            TestKey = SkeinTestKey,
            KnownAnswers = variant == Skein1024TestVariant.Hash_1024
                ? [MessageDigestKnownAnswer.Empty(Skein1024_1024_EmptyHash)]
                : [],
        };

    /// <inheritdoc />
    protected override IReadOnlyList<MessageDigestKnownAnswer> GetKeyedKnownAnswers(Skein1024TestVariant variant) =>
        Skein1024KnownAnswers.For(variant);

    /// <inheritdoc />
    protected override Skein1024 CreateAlgorithm(Skein1024TestVariant variant)
    {
        var algorithm = new Skein1024(OutputBitsFor(variant));
        if (IsMacVariant(variant))
            algorithm.Key = SkeinTestKey;
        return algorithm;
    }

    /// <inheritdoc />
    /// <remarks>
    /// See <see cref="Skein256Tests.GetExpectedHashesForIncrementalInput" /> — the Skein 1.3 KAT files do not
    /// publish a dense byte-by-byte sequence; the data-driven keyed KAT path supplies the reference coverage.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Skein1024TestVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyList<ulong>? GetExpectedAppendixBChainingValue(Skein1024TestVariant variant) =>
        variant switch
        {
            Skein1024TestVariant.Hash_384 => Skein1024AppendixBIVs.Skein1024_384,
            Skein1024TestVariant.Hash_512 => Skein1024AppendixBIVs.Skein1024_512,
            Skein1024TestVariant.Hash_1024 => Skein1024AppendixBIVs.Skein1024_1024,
            _ => null,
        };

    /// <summary>
    /// Translates a <see cref="Skein1024TestVariant" /> into the digest size in bits embedded in the variant name.
    /// </summary>
    private static int OutputBitsFor(Skein1024TestVariant variant) =>
        variant switch
        {
            Skein1024TestVariant.Hash_384 or Skein1024TestVariant.Mac_384 => 384,
            Skein1024TestVariant.Hash_512 or Skein1024TestVariant.Mac_512 => 512,
            Skein1024TestVariant.Hash_1024 or Skein1024TestVariant.Mac_1024 => 1024,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Skein1024 variant."),
        };
}
