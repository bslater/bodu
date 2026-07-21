// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdditiveDigestTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Security.Cryptography.Samples.CustomHash;

namespace Bodu.Security.Cryptography.Samples.CustomHash;

/// <summary>
/// Drives the library's shared <see cref="BlockHashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> contract
/// against the sample's <see cref="AdditiveDigest" />. This is the pattern for any consumer-authored
/// <c>BlockHashAlgorithm</c>: supply a <see cref="HashAlgorithmSpecification" />, a factory, and the dense
/// incremental-input digest table, and inherit the full block-buffered hashing contract — residual-buffer
/// accumulation, block-alignment parity, padded-final-block correctness, streaming/async parity, disposal
/// state, and property reflection — the same bar Tiger, Whirlpool, and BLAKE2b are held to.
/// </summary>
[TestClass]
public sealed class AdditiveDigestTests
    : BlockHashAlgorithmTests<AdditiveDigestTests, AdditiveDigest, AdditiveDigest.Variant>
{
    /// <summary>
    /// The curated known-answer digests produced by <see cref="AdditiveDigest" /> over the canonical shared
    /// inputs. These are self-computed (the sample hash is not a standardized primitive) and pin the output
    /// so any change to the algorithm surfaces as a test failure.
    /// </summary>
    private static readonly HashAlgorithmSpecification Specification = new()
    {
        HashSize = 128,
        HashBlockSize = 16,
        InputBlockSize = 1,
        OutputBlockSize = 1,
        CanReuseTransform = true,
        CanTransformMultipleBlocks = true,
        LongInputLength = 256,
        BoundaryLengths = [1, 15, 16, 17, 32, 64],
        MinNonZeroBytesForLongInput = 8,
        KnownAnswers =
        [
            MessageDigestKnownAnswer.Empty("caac1613ec962f5c9196d874d73d3458"),
            MessageDigestKnownAnswer.Abc("41c67090534c671053ff93ec729f93b4"),
            MessageDigestKnownAnswer.QuickBrownFox("ff043f423a5e05d9382e8badcba165dd"),
            MessageDigestKnownAnswer.Zeros16("b47529bba1b44e572919c4624415fee9"),
            MessageDigestKnownAnswer.Sequential0To255("3c3c878d386a8b78d4e2fb2f079c81fe"),
        ],
    };

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(AdditiveDigest.Variant variant) =>
        Specification;

    /// <inheritdoc />
    protected override AdditiveDigest CreateAlgorithm() =>
        new();

    /// <inheritdoc />
    protected override AdditiveDigest CreateAlgorithm(AdditiveDigest.Variant variant) =>
        new();

    /// <summary>
    /// The digests produced by hashing the first <c>i</c> bytes of the sequence <c>0x00, 0x01, 0x02, …</c>
    /// for <c>i</c> from 0 (empty) through 17, spanning the residual-buffer and 16-byte block boundaries.
    /// Entry 0 must equal the <c>"Empty"</c> known answer, which the base contract asserts.
    /// </summary>
    /// <param name="variant">The variant under test (the algorithm has only one).</param>
    /// <returns>The ordered hex digests for incremental input lengths 0 through 17.</returns>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(AdditiveDigest.Variant variant) =>
    [
        "caac1613ec962f5c9196d874d73d3458",
        "ae1206864cafc7c62dd51af9f3e4b5b3",
        "a89b62100af0548326b645c8110ab859",
        "54187ab9d31ea8eb337b41d9351cdfba",
        "6ce4055b27d92774d1899485c23730d3",
        "c538e880e023c21ec4cdc2be41b276a8",
        "e76ba7a6294f2311f504bbfa7ea1c49a",
        "1453257f71ca51c296b8d7a61b3a613a",
        "990a3fb151985f3a8851b8106ecaa63a",
        "f5a6446b6e387028dde6819699e5f5b7",
        "5f61c778fc504b124b9e017ffe398357",
        "c9257418b429b3f9ceb26dc66fdff4bd",
        "639feec9701ed472f49d34cc66d90738",
        "92ce44afbbc3e0602961b4cec58653b4",
        "1c8d108fca661cfb18498e1a41e0152f",
        "16c89b7dfe78c35e1dc481bd711f5ae7",
        "992567b5f8ac67d9b4123fadd430f70e",
        "23ef8186384d059551df3c03123f9a60",
    ];
}
