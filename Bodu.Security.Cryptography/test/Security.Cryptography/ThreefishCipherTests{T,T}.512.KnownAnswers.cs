// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishCipherTests{T,T}.512.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Threefish512Cipher" /> known-answer test vectors. Mirrors the layout of
/// <see cref="Threefish256CipherTests" /> at the wider 512-bit block size.
/// </summary>
/// <remarks>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> — the
/// harness's incremental-byte default (key bytes 0x10..0x4F, tweak bytes 0x00..0x0F, descending plaintext
/// FF..C0). Equivalence with the Skein 1.3 / NIST SHA-3 submission reference was verified at the Threefish-256
/// layer (see <see cref="Threefish256CipherTests" />); the wider-block 512-bit variant uses the identical
/// Threefish round structure so the same byte-stream / word-value mapping applies here.
/// </remarks>
internal sealed partial class Threefish512CipherTests
{
    private const string ProfileInTreeRegression = "Skein 1.3 / NIST SHA-3 reference (verified equivalent at Threefish-256 layer)";

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish512_ZeroKeyZeroTweak_ZeroPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = new byte[64],
            Ciphertext = Convert.FromHexString(
                "B1A2BBC6EF6025BC40EB3822161F36E375D1BB0AEE3186FBD19E47C5D479947B" +
                "7BC2F8586E35F0CFF7E7F03084B0B7B1F1AB3961A580A3E97EB41EA14A6D7BBE"),
            Key = new byte[64],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish512_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0"),
            Ciphertext = Convert.FromHexString(
                "E304439626D45A2CB401CAD8D636249A6338330EB06D45DD8B36B90E97254779" +
                "272A0A8D99463504784420EA18C9A725AF11DFFEA10162348927673D5C1CAF3D"),
            Key = TestHelpers.GenerateIncrementalByteSequence(0x10, 64),
            Tweak = TestHelpers.GenerateIncrementalByteSequence(0x00, 16),
        },
    ];

    /// <summary>
    /// Returns the curated KAT vector for <paramref name="variant" />.
    /// </summary>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(TweakableBlockCipherVariant variant) => variant switch
    {
        TweakableBlockCipherVariant.ZeroedKeyAndTweak => ZeroedKeyAndTweakKnownAnswers,
        TweakableBlockCipherVariant.DefaultKeyAndTweak => DefaultKeyAndTweakKnownAnswers,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };
}
