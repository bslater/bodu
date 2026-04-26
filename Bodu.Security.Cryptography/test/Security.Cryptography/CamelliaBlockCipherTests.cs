// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="CamelliaBlockCipher" /> implementation against the three key-size variants (128, 192,
/// and 256 bits), including RFC 3713 known-answer test vectors and round-trip correctness checks.
/// </summary>
[TestClass]
internal sealed partial class CamelliaBlockCipherTests
    : BlockCipherTests<CamelliaBlockCipherTests, CamelliaBlockCipher, CamelliaTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(CamelliaTestVariant variant)
    {
        int keySize = variant switch
        {
            CamelliaTestVariant.Key128 => 16,
            CamelliaTestVariant.Key192 => 24,
            CamelliaTestVariant.Key256 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

        return new BlockCipherSpecification
        {
            BlockSize = 16,
            KeySize = keySize,
            TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, keySize),
        };
    }

    /// <inheritdoc />
    public override IEnumerable<CamelliaTestVariant> GetBlockCipherVariants() => new[]
    {
        CamelliaTestVariant.Key128,
        CamelliaTestVariant.Key192,
        CamelliaTestVariant.Key256,
    };

    /// <inheritdoc />
    protected override CamelliaBlockCipher CreateBlockCipher(CamelliaTestVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return new CamelliaBlockCipher(spec.TestKey!);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(CamelliaTestVariant variant)
    {
        // RFC 3713 Appendix A — single-block ECB vectors (no padding, no IV).
        // Key and plaintext: 0123456789ABCDEF FEDCBA9876543210
        // For 192/256-bit keys the same 128-bit block is encrypted; extra key bytes are appended.
        return variant switch
        {
            CamelliaTestVariant.Key128 => GetKey128KnownAnswerTests(),
            CamelliaTestVariant.Key192 => GetKey192KnownAnswerTests(),
            CamelliaTestVariant.Key256 => GetKey256KnownAnswerTests(),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    private static IEnumerable<KnownAnswerTest> GetKey128KnownAnswerTests()
    {
        // RFC 3713 Appendix A.1 — single-block ECB encrypt, no padding or IV.
        yield return CreateKnownAnswerTest(
            "Camellia-128 / RFC 3713 A.1",
            "0123456789ABCDEFFEDCBA9876543210",
            "0123456789ABCDEFFEDCBA9876543210",
            "67673138549669730857065648EABE43");
    }

    private static IEnumerable<KnownAnswerTest> GetKey192KnownAnswerTests()
    {
        // RFC 3713 Appendix A.2
        yield return CreateKnownAnswerTest(
            "Camellia-192 / RFC 3713 A.2",
            "0123456789ABCDEFFEDCBA9876543210" + "0011223344556677",
            "0123456789ABCDEFFEDCBA9876543210",
            "B4993401B3E996F84EE5CEE7D79B09B9");
    }

    private static IEnumerable<KnownAnswerTest> GetKey256KnownAnswerTests()
    {
        // RFC 3713 Appendix A.3
        yield return CreateKnownAnswerTest(
            "Camellia-256 / RFC 3713 A.3",
            "0123456789ABCDEFFEDCBA9876543210" + "0011223344556677" + "8899AABBCCDDEEFF",
            "0123456789ABCDEFFEDCBA9876543210",
            "9ACC237DFF16D76C20EF7C919E3A7509");
    }

    private static KnownAnswerTest CreateKnownAnswerTest(
        string name,
        string key,
        string input,
        string expectedOutput)
    {
        byte[] keyBytes = Convert.FromHexString(key);

        return new KnownAnswerTest
        {
            Name = name,
            Input = Convert.FromHexString(input),
            ExpectedOutput = Convert.FromHexString(expectedOutput),
            Parameters = new Dictionary<string, object>
            {
                ["Key"] = keyBytes,
                ["KeySize"] = keyBytes.Length * 8,
                ["Mode"] = "ECB",
                ["Padding"] = "None",
            },
            CipherFactory = () => new CamelliaBlockCipher(keyBytes),
        };
    }
}
