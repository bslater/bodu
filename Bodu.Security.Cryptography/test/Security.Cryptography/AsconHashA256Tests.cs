// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHashA256Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconHashA256" /> hash algorithm.
/// </summary>
[TestClass]
public partial class AsconHashA256Tests
    : BlockHashAlgorithmTests<AsconHashA256Tests, AsconHashA256, AsconHashA256Tests.Variant>
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
    /// Identifies the single configuration variant of <see cref="AsconHashA256" />.
    /// </summary>
    public enum Variant
    {
        /// <summary>The standard ASCON-HASHA256 configuration as defined in NIST SP 800-232.</summary>
        Default,
    }

    /// <inheritdoc />
    public override IEnumerable<Variant> GetHashAlgorithmVariants() => [Variant.Default];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(Variant variant) => Specification;

    /// <inheritdoc />
    protected override AsconHashA256 CreateAlgorithm(Variant variant) => new AsconHashA256();

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Variant variant) =>
        new Dictionary<string, string>
        {
            // TODO: populate with known-answer test vectors from NIST SP 800-232 once the implementation has been
            // verified against the ASCON v1.2 reference implementation.
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Variant variant) => [];
}
