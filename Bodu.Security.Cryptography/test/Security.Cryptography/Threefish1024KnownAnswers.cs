// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish1024KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Threefish1024Cipher" /> known-answer test vectors used by
/// <see cref="Threefish1024CipherTests" />. Mirrors the layout of <see cref="Threefish256KnownAnswers" />
/// and <see cref="Threefish512KnownAnswers" /> at the widest 1024-bit block size.
/// </summary>
/// <remarks>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> —
/// the harness's incremental-byte default (key bytes 0x10..0x8F, tweak bytes 0x00..0x0F, descending
/// plaintext FF..80). Equivalence with the Skein 1.3 / NIST SHA-3 submission reference was verified at
/// the Threefish-256 layer (see <see cref="Threefish256KnownAnswers" />); the wider-block 1024-bit
/// variant uses the identical Threefish round structure so the same byte-stream / word-value mapping
/// applies here.
/// </remarks>
internal static class Threefish1024KnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vector for <paramref name="variant" />.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(TweakableBlockCipherVariant variant) => variant switch
    {
        TweakableBlockCipherVariant.ZeroedKeyAndTweak => ZeroedKeyAndTweak,
        TweakableBlockCipherVariant.DefaultKeyAndTweak => DefaultKeyAndTweak,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string ProfileInTreeRegression = "Skein 1.3 / NIST SHA-3 reference (verified equivalent at Threefish-256 layer)";

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweak =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish1024_ZeroKeyZeroTweak_ZeroPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = new byte[128],
            Ciphertext = Convert.FromHexString(
                "F05C3D0A3D05B304F785DDC7D1E036015C8AA76E2F217B06C6E1544C0BC1A90D" +
                "F0ACCB9473C24E0FD54FEA68057F43329CB454761D6DF5CF7B2E9B3614FBD5A2" +
                "0B2E4760B40603540D82EABC5482C171C832AFBE68406BC39500367A592943FA" +
                "9A5B4A43286CA3C4CF46104B443143D560A4B230488311DF4FEEF7E1DFE8391E"),
            Key = new byte[128],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweak =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish1024_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
                "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
                "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180"),
            Ciphertext = Convert.FromHexString(
                "A6654DDBD73CC3B05DD777105AA849BCE49372EAAFFC5568D254771BAB85531C" +
                "94F780E7FFAAE430D5D8AF8C70EEBBE1760F3B42B737A89CB363490D670314BD" +
                "8AA41EE63C2E1F45FBD477922F8360B388D6125EA6C7AF0AD7056D01796E90C8" +
                "3313F4150A5716B30ED5F569288AE974CE2B4347926FCE57DE44512177DD7CDE"),
            Key = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 128),
            Tweak = CryptoTestUtilities.CreateIncrementalByteSequence(0x00, 16),
        },
    ];
}
