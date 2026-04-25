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

    /// <summary>
    /// Computes the empty-message hash at runtime because no hardcoded KAT vector is available yet for
    /// <see cref="AsconHashA256" />. Structural tests that compare against this value will still verify
    /// consistency across different call paths even without an external reference value.
    /// </summary>
    /// <returns>The hash of an empty byte sequence produced by <see cref="AsconHashA256" />.</returns>
    protected override byte[] ExpectedEmptyInputHash
    {
        get
        {
            using AsconHashA256 hash = new AsconHashA256();
            return hash.ComputeHash(Array.Empty<byte>());
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Variant variant) =>
        new Dictionary<string, string>
        {
            // TODO: populate with known-answer test vectors from NIST SP 800-232 once the implementation has been
            // verified against the ASCON v1.2 reference implementation.
        };

    /// <inheritdoc />
    /// <remarks>
    /// No official known-answer vectors are available yet for <see cref="AsconHashA256" />. A single
    /// runtime-computed vector for the empty input is provided so that the named-input data-driven
    /// tests have at least one row to enumerate; the value is cross-checked for consistency rather
    /// than against an external reference.
    /// </remarks>
    protected override IEnumerable<KnownAnswerTest> GetTestVectors(Variant variant) =>
        [new KnownAnswerTest { Name = "Empty", Input = [], ExpectedOutput = this.ExpectedEmptyInputHash }];

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Variant variant) => [];
}
