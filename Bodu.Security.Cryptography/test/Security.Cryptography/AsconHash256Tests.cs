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
    : BlockHashAlgorithmTests<AsconHash256Tests, AsconHash256, SingleTestVariant>
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
            // The Empty slot and the GetExpectedHashesForIncrementalInput sequence below come directly
            // from the ASCON reference implementation (ascon-c, LWC_HASH_KAT_128_256.txt). The remaining
            // typed-slot digests are computed from the same reference algorithm — NIST SP 800-232,
            // ASCON-HASH256 — applied to the canonical shared inputs declared in
            // HashAlgorithmSharedInputs. They are cross-checked against the published incremental KAT
            // (the entries for input lengths 0..9 below) to confirm the permutation, IV, padding and
            // squeezing schedule are bit-exact with the reference.
            Empty = "0B3BE5850F2F6B98CAF29F8FDEA89B64A1FA70AA249B8F839BD53BAA304D92B2",
            Abc = "AF5724830636A475C9843106DC4EE6414DC893635A45A9DE95805C36F8596DB2",
            QuickBrownFox = "23414503BF4BDE7AD0E85AEC94C22AE2D7CD807996B537F9564FC2974053F139",
            Zeros16 = "2AD51185719429533DACA898C69BA9D682088B18D2D0C8AB780FBDFA3D6EA56B",
            Sequential0To255 = "ADA496E2C0ADE829F37832A8BA34CF6059DFFBB3BEBA88CA5DED3363914EA69A",
        },
    };

    /// <inheritdoc />
    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => [SingleTestVariant.Default];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => Specification;

    /// <inheritdoc />
    protected override AsconHash256 CreateAlgorithm(SingleTestVariant variant) => new AsconHash256();

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        // Known-answer test vectors sourced from the ASCON reference implementation (ascon-c, LWC_HASH_KAT_128_256.txt).
        // Each entry is the hash of the sequential input [0x00, 0x01, ..., 0x(n-1)] for n = 0..65.
        "0B3BE5850F2F6B98CAF29F8FDEA89B64A1FA70AA249B8F839BD53BAA304D92B2", "0728621035AF3ED2BCA03BF6FDE900F9456F5330E4B5EE23E7F6A1E70291BC80", "6115E7C9C4081C2797FC8FE1BC57A836AFA1C5381E556DD583860CA2DFB48DD2", "265AB89A609F5A05DCA57E83FBBA700F9A2D2C4211BA4CC9F0A1A369E17B915C",
        "D7E4C7ED9B8A325CD08B9EF259F8877054ECD8304FE1B2D7FD847137DF6727EE", "C7B28962D4F5C2211F466F83D3C57AE1504387E2A326949747A8376447A6BB51", "DC0C6748AF8FFE63E1084AA3E5786A194685C88C21348B29E184FB50409703BC", "3E4D273BA69B3B9C53216107E88B75CDBEEDBCBF8FAF0219C3928AB62B116577",
        "B88E497AE8E6FB641B87EF622EB8F2FCA0ED95383F7FFEBE167ACF1099BA764F", "94269C30E0296E1EC86655041841823EFA1927F520FD58C8E9BCE6197878C1A6", 
    ];
}
