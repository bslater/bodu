// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Skein512" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Skein512Tests
    : Security.Cryptography.SkeinTests<Skein512Tests, Skein512>
{
    private const string Skein512EmptyHash =
        "BC5B4C50925519C290CC634277AE3D6257212395CBA733BBAD37A4AF0FA06AF4" +
        "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBA7A";

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 512,
        InputBlockSize = 64,
        OutputBlockSize = 64,
        IsStateless = false,
        LongInputLength = 256,
        BoundaryLengths = [1, 16, 64, 128, 256],
        MinKeyLength = 0,
        MaxKeyLength = Skein<Skein512>.MaxKeySizeBytes,
        ValidKeyLengths = [0, 16, 32, 64, 128, 256, Skein<Skein512>.MaxKeySizeBytes],
        TestKey = Array.Empty<byte>(),
    };

    /// <inheritdoc />
    protected override Skein512 CreateAlgorithm(SingleTestVariant variant) => new Skein512();

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
            ["Empty"] = Skein512EmptyHash,
        };
}
