// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests{T,T}.1024.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test;
namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Serpent1024Cipher" /> known-answer test vectors. Each row pins a (key, tweak, plaintext,
/// ciphertext) tuple so the vectors remain self-contained: subsequent changes to the wide-block construction
/// cannot silently invalidate the captured ciphertext.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-1024 is a non-standard tweakable Serpent construction developed for this library — it has no
/// externally published reference vectors. The captured ciphertexts were cross-validated against an independent
/// Python port of the wide-block round function (see <c>tools/cipher-vectors/wide_serpent.py</c>), which is
/// hand-translated from the C# source and exercises the same Serpent S-boxes, bitsliced linear transform, prekey
/// recurrence, cross-lane rotation, and five-word tweak schedule. Both implementations agree on the rows below.
/// </para>
/// <para>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> — the
/// harness's incremental-byte default (key bytes 0x10..0x8F, tweak bytes 0x00..0x0F, descending plaintext
/// FF..80).
/// </para>
/// </remarks>
internal sealed partial class Serpent1024CipherTests
{
    private static readonly KatProvenance ProfileCrossValidated =
        KatProvenance.DerivedOracle("Cross-validated against tools/cipher-vectors/wide_serpent.py (independent Python port)");

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent1024_ZeroKeyZeroTweak_ZeroPlaintext",
            Provenance = ProfileCrossValidated,
            Plaintext = new byte[128],
            Ciphertext = Convert.FromHexString(
                "1A1751E17D389038A39D961EDA005F2280B1B45BA3BAD9C42C66C478FF09FE50" +
                "BA490094A44DCA64CBAE2DD0D68F391F51BCCD2BB527230A66A48263593D22F1" +
                "1377805B067C6E4153AA13CB1A6ED85AEB17752D3913FB95B65CF52EF04B6EA5" +
                "8A41B556F9A2E2B84425A3998058E93E6F8E1CD51A86061B3F8F96998E864D89"),
            Key = new byte[128],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent1024_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Provenance = ProfileCrossValidated,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
                "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
                "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180"),
            Ciphertext = Convert.FromHexString(
                "4CC61C560A0A6C04A51E088B59A9DDC60EFFCC179B5FDABE764CB76870FDDE90" +
                "099704F6E0F2E4E63FF9535070F42212CD54EDF5EB6F4E4BE6B5EF39D9A2D1AC" +
                "1C393706E562ADB83E1E12B8DDEEB1BC123CB4169D02A5F7FD1575FE08EE9956" +
                "059E15032183F5DED199A3659F1AC777BCAC4E08E28E0DDBF2CE7E6D3B939261"),
            Key = TestHelpers.GenerateIncrementalByteSequence(0x10, 128),
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
