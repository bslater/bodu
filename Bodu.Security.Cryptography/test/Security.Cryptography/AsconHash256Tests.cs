// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconHash256" /> hash algorithm.
/// </summary>
[TestClass]
public partial class AsconHash256Tests
    : BlockHashAlgorithmTests<AsconHash256Tests, AsconHash256, AsconHash256Tests.Variant>
{
    private static readonly HashAlgorithmSpecification Specification = new()
    {
        HashSize = 256,
        InputBlockSize = 8,
        OutputBlockSize = 32,
        LongInputLength = 256,
        BoundaryLengths = [1, 8, 16, 64],
        MinNonZeroBytesForLongInput = 28,
    };

    /// <summary>
    /// Identifies the single configuration variant of <see cref="AsconHash256" />.
    /// </summary>
    public enum Variant
    {
        /// <summary>The standard ASCON-HASH256 configuration as defined in NIST SP 800-232.</summary>
        Default,
    }

    /// <inheritdoc />
    public override IEnumerable<Variant> GetHashAlgorithmVariants() => [Variant.Default];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Variant variant) => Specification;

    /// <inheritdoc />
    protected override AsconHash256 CreateAlgorithm(Variant variant) => new AsconHash256();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Variant variant) =>
        new Dictionary<string, string>
        {
            // Known-answer test vectors sourced from NIST SP 800-232 and the ASCON v1.2 reference implementation.
            ["Empty"] = "7346BC14F036E87AE03D0997913088F5F68411434B3CF8B54FA796A80D251F91",
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Variant variant) => [];
}
