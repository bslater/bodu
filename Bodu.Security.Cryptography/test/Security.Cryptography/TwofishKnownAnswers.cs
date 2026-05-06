// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="TwofishBlockCipher" /> known-answer test vectors used by
/// <see cref="TwofishBlockCipherTests" />, transcribed from the Twofish AES submission ECB intermediate-values
/// test vectors (<c>ecb_ival.txt</c>) hosted on Bruce Schneier's site. Each per-variant collection covers the
/// canonical iteration steps <c>I=1..5</c> plus the I=10 marker — sufficient to detect any divergence in the
/// key schedule, MDS matrix, or whitening logic against the reference implementation.
/// </summary>
/// <remarks>
/// All vectors target single-block Twofish ECB encryption with no padding or IV. The intermediate-values format
/// chains plaintext and key material across iterations: each row's plaintext is the previous row's ciphertext,
/// and for 192- and 256-bit key sizes the key is rotated with prior ciphertext bytes per the AES KAT
/// specification.
/// </remarks>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2015/12/ecb_ival.txt">ecb_ival.txt — Twofish ECB intermediate-values vectors</seealso>
internal static class TwofishKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Each key size carries six vectors
    /// drawn from iteration steps 1–5 and 10 of the Twofish AES submission corpus.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(BlockCipherKeyVariant variant) => variant switch
    {
        BlockCipherKeyVariant.Key128 => Key128,
        BlockCipherKeyVariant.Key192 => Key192,
        BlockCipherKeyVariant.Key256 => Key256,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string ZeroBlock = "00000000000000000000000000000000";

    private const string ZeroKey128 = "00000000000000000000000000000000";

    private const string ZeroKey192 = "000000000000000000000000000000000000000000000000";

    private const string ZeroKey256 = "0000000000000000000000000000000000000000000000000000000000000000";

    private const string ProfileAesSubmission = "Twofish AES submission ecb_ival.txt";

    private static readonly BlockCipherKnownAnswer[] Key128 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I1_ZeroKeyZeroPlaintext",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString(ZeroBlock),
            Ciphertext = Convert.FromHexString("9F589F5CF6122C32B6BFEC2F2AE8C35A"),
            Key = Convert.FromHexString(ZeroKey128),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I2",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("9F589F5CF6122C32B6BFEC2F2AE8C35A"),
            Ciphertext = Convert.FromHexString("D491DB16E7B1C39E86CB086B789F5419"),
            Key = Convert.FromHexString(ZeroKey128),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I3",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("D491DB16E7B1C39E86CB086B789F5419"),
            Ciphertext = Convert.FromHexString("019F9809DE1711858FAAC3A3BA20FBC3"),
            Key = Convert.FromHexString("9F589F5CF6122C32B6BFEC2F2AE8C35A"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I4",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("019F9809DE1711858FAAC3A3BA20FBC3"),
            Ciphertext = Convert.FromHexString("6363977DE839486297E661C6C9D668EB"),
            Key = Convert.FromHexString("D491DB16E7B1C39E86CB086B789F5419"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I5",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("6363977DE839486297E661C6C9D668EB"),
            Ciphertext = Convert.FromHexString("816D5BD0FAE35342BF2A7412C246F752"),
            Key = Convert.FromHexString("019F9809DE1711858FAAC3A3BA20FBC3"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish128_I10",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("28530B358C1B42EF277DE6D4407FC591"),
            Ciphertext = Convert.FromHexString("8A8AB983310ED78C8C0ECDE030B8DCA4"),
            Key = Convert.FromHexString("34C8A5FB2D3D08A170D120AC6D26DBFA"),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key192 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I1_ZeroKeyZeroPlaintext",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString(ZeroBlock),
            Ciphertext = Convert.FromHexString("EFA71F788965BD4453F860178FC19101"),
            Key = Convert.FromHexString(ZeroKey192),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I2",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("EFA71F788965BD4453F860178FC19101"),
            Ciphertext = Convert.FromHexString("88B2B2706B105E36B446BB6D731A1E88"),
            Key = Convert.FromHexString(ZeroKey192),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I3",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("88B2B2706B105E36B446BB6D731A1E88"),
            Ciphertext = Convert.FromHexString("39DA69D6BA4997D585B6DC073CA341B2"),
            Key = Convert.FromHexString("EFA71F788965BD4453F860178FC191010000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I4",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("39DA69D6BA4997D585B6DC073CA341B2"),
            Ciphertext = Convert.FromHexString("182B02D81497EA45F9DAACDC29193A65"),
            Key = Convert.FromHexString("88B2B2706B105E36B446BB6D731A1E88EFA71F788965BD44"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I5",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("182B02D81497EA45F9DAACDC29193A65"),
            Ciphertext = Convert.FromHexString("7AFF7A70CA2FF28AC31DD8AE5DAAAB63"),
            Key = Convert.FromHexString("39DA69D6BA4997D585B6DC073CA341B288B2B2706B105E36"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish192_I10",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("893FD67B98C550073571BD631263FC78"),
            Ciphertext = Convert.FromHexString("16434FC9C8841A63D58700B5578E8F67"),
            Key = Convert.FromHexString("AE8109BFDA85C1F2C5038B34ED691BFF3AF6F7CE5BD35EF1"),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key256 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I1_ZeroKeyZeroPlaintext",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString(ZeroBlock),
            Ciphertext = Convert.FromHexString("57FF739D4DC92C1BD7FC01700CC8216F"),
            Key = Convert.FromHexString(ZeroKey256),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I2",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("57FF739D4DC92C1BD7FC01700CC8216F"),
            Ciphertext = Convert.FromHexString("D43BB7556EA32E46F2A282B7D45B4E0D"),
            Key = Convert.FromHexString(ZeroKey256),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I3",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("D43BB7556EA32E46F2A282B7D45B4E0D"),
            Ciphertext = Convert.FromHexString("90AFE91BB288544F2C32DC239B2635E6"),
            Key = Convert.FromHexString("57FF739D4DC92C1BD7FC01700CC8216F00000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I4",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("90AFE91BB288544F2C32DC239B2635E6"),
            Ciphertext = Convert.FromHexString("6CB4561C40BF0A9705931CB6D408E7FA"),
            Key = Convert.FromHexString("D43BB7556EA32E46F2A282B7D45B4E0D57FF739D4DC92C1BD7FC01700CC8216F"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I5",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("6CB4561C40BF0A9705931CB6D408E7FA"),
            Ciphertext = Convert.FromHexString("3059D6D61753B958D92F4781C8640E58"),
            Key = Convert.FromHexString("90AFE91BB288544F2C32DC239B2635E6D43BB7556EA32E46F2A282B7D45B4E0D"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Twofish256_I10",
            Profile = ProfileAesSubmission,
            Plaintext = Convert.FromHexString("C5A3E7CEE0F1B7260528A68FB4EA05F2"),
            Ciphertext = Convert.FromHexString("43D5CEC327B24AB90AD34A79D0469151"),
            Key = Convert.FromHexString("DC096BCD99FC72F79936D4C748E75AF75AB67A5F8539A4A5FD9F0373BA463466"),
        },
    ];
}
