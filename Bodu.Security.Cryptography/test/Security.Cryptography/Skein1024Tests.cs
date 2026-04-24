// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein1024" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Skein1024Tests
    : Security.Cryptography.SkeinTests<Skein1024Tests, Skein1024>
{
    private const string Skein1024EmptyHash =
        "0FFF9563BB3279289227AC77D319B6FFF8D7E9F09DA1247B72A0A265CD6D2A62" +
        "645AD547ED8193DB48CFF847C06494A03F55666D3B47EB4C20456C9373C86297" +
        "D630D5578EBD34CB40991578F9F52B18003EFA35D3DA6553FF35DB91B81AB890" +
        "BEC1B189B7F52CB2A783EBB7D823D725B0B4A71F6824E88F68F982EEFC6D19C6";

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 1024,
        InputBlockSize = 128,
        OutputBlockSize = 128,
        IsStateless = false,
        LongInputLength = 512,
        BoundaryLengths = [1, 16, 128, 256, 512],
        MinKeyLength = 16,
        MaxKeyLength = Skein<Skein1024>.MaxKeySizeBytes,
        ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein1024>.MaxKeySizeBytes],
        TestKey = SkeinTestKey,
    };

    /// <inheritdoc />
    protected override Skein1024 CreateAlgorithm(SingleTestVariant variant) => new Skein1024();

    /// <inheritdoc />
    /// <remarks>
    /// See <see cref="Skein256Tests.GetExpectedHashesForIncrementalInput" /> — authoritative incremental vectors
    /// are still pending for the Skein family; until they land the incremental test remains inconclusive.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
        new Dictionary<string, string>
        {
            ["Empty"] = Skein1024EmptyHash,
        };
}
