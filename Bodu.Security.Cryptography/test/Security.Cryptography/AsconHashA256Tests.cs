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
        HashBlockSize = 8,
        LongInputLength = 256,
        BoundaryLengths = [1, 8, 16, 64],
        MinNonZeroBytesForLongInput = 28,
        KnownAnswers = new()
        {
            // No official known-answer vectors are available yet for AsconHashA256. The empty-input value
            // is runtime-computed and cross-checked for consistency rather than against an external reference.
            Empty = "19BF587ED2116F38D0FDD852ACE7C83C3DA1D70E2503A67F278C4C612F4ABC39",
        },
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
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Variant variant) => new[]
    {
        "19BF587ED2116F38D0FDD852ACE7C83C3DA1D70E2503A67F278C4C612F4ABC39", "166D100F8FF608A7276E90D332135DE9143FBE1355F0643E1C62883F536B66C7", "EDEA4E90FE70D1855921190FF429365B8B56D53356F0DBBCCE36D614D629EC7A", "A72FD29B414A1B319CDAB35A6ECF2C1DBBE4AD48B6CC3AF487F3873922ED33FB",
        "F578AD2CD5EF17F940CF2D35B20C830EF8942AE1BAAE30FF9477F712B1A9E59E", "BBAE00F4EBA31C687DA603B75647F3ECC35AD64C0AA6BAEC1018F2D2AC400A0E", "5E1751683629212B4BED86699D90CF11D78199ABA032554EF9640160220AE037", "DFD27657CE0F5DA939625E82E095AE7E2F80DC87A96262C56708AC82E00F7833",
        "E92196427D3B23FEF4B11DA39368A750259980F493E7428A2530DA36FD90B8DF", "C9E05D3060F4ABFEB1007A900946A3A3617B42FCF99852C9CBD4CD1107E20FE4", 
    };
}