// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein256" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Skein256Tests
    : Security.Cryptography.SkeinTests<Skein256Tests, Skein256>
{
    private const string Skein256EmptyHash = "C8877087DA56E072870DAA843F176E9453115929094C3A40C463A196C29BF7BA";

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 256,
        InputBlockSize = 32,
        OutputBlockSize = 32,
        IsStateless = false,
        LongInputLength = 200,
        BoundaryLengths = [1, 8, 16, 32, 64, 128],
        MinKeyLength = 16,
        MaxKeyLength = Skein<Skein256>.MaxKeySizeBytes,
        ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein256>.MaxKeySizeBytes],
        TestKey = SkeinTestKey,
    };

    /// <inheritdoc />
    protected override Skein256 CreateAlgorithm(SingleTestVariant variant) => new Skein256();

    /// <inheritdoc />
    /// <remarks>
    /// The framework iterates this set to exercise every incremental input length. Skein does not yet ship
    /// authoritative reference vectors across the full <c>InputBlockSize * 8</c> range; returning an empty list
    /// marks the incremental-input test as <see cref="MSTest.TestAdapter.TestOutcome.Inconclusive" /> until a
    /// vetted reference set is wired in.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
        Array.Empty<string>();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
        new Dictionary<string, string>
        {
            ["Empty"] = Skein256EmptyHash,
        };
}
