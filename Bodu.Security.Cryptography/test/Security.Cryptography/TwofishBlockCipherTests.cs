// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class TwofishBlockCipherTests
    : BlockCipherTests<TwofishBlockCipherTests, TwofishBlockCipher, TwofishTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TwofishTestVariant variant)
    {
        var keySize = variant switch
        {
            TwofishTestVariant.Key128 => 16,
            TwofishTestVariant.Key192 => 24,
            TwofishTestVariant.Key256 => 32,
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
    public override IEnumerable<TwofishTestVariant> GetBlockCipherVariants() => new[]
    {
        TwofishTestVariant.Key128,
        TwofishTestVariant.Key192,
        TwofishTestVariant.Key256,
    };

    /// <inheritdoc />
    protected override TwofishBlockCipher CreateBlockCipher(TwofishTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new TwofishBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TwofishTestVariant variant)
    {
        // Published Twofish AES submission ECB intermediate-value vectors:
        // https://www.schneier.com/wp-content/uploads/2015/12/ecb_ival.txt
        //
        // These vectors are raw single-block Twofish ECB encryptions and do not include
        // padding, IV handling, or cipher-mode feedback.

        return variant switch
        {
            TwofishTestVariant.Key128 => GetKey128KnownAnswerTests(),
            TwofishTestVariant.Key192 => GetKey192KnownAnswerTests(),
            TwofishTestVariant.Key256 => GetKey256KnownAnswerTests(),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }

    private static IEnumerable<KnownAnswerTest> GetKey128KnownAnswerTests()
    {
        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=1 / zero key, zero plaintext",
            "00000000000000000000000000000000",
            "00000000000000000000000000000000",
            "9F589F5CF6122C32B6BFEC2F2AE8C35A");

        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=2",
            "00000000000000000000000000000000",
            "9F589F5CF6122C32B6BFEC2F2AE8C35A",
            "D491DB16E7B1C39E86CB086B789F5419");

        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=3",
            "9F589F5CF6122C32B6BFEC2F2AE8C35A",
            "D491DB16E7B1C39E86CB086B789F5419",
            "019F9809DE1711858FAAC3A3BA20FBC3");

        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=4",
            "D491DB16E7B1C39E86CB086B789F5419",
            "019F9809DE1711858FAAC3A3BA20FBC3",
            "6363977DE839486297E661C6C9D668EB");

        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=5",
            "019F9809DE1711858FAAC3A3BA20FBC3",
            "6363977DE839486297E661C6C9D668EB",
            "816D5BD0FAE35342BF2A7412C246F752");

        yield return CreateKnownAnswerTest(
            "Twofish-128 / I=10",
            "34C8A5FB2D3D08A170D120AC6D26DBFA",
            "28530B358C1B42EF277DE6D4407FC591",
            "8A8AB983310ED78C8C0ECDE030B8DCA4");
    }

    private static IEnumerable<KnownAnswerTest> GetKey192KnownAnswerTests()
    {
        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=1 / zero key, zero plaintext",
            "000000000000000000000000000000000000000000000000",
            "00000000000000000000000000000000",
            "EFA71F788965BD4453F860178FC19101");

        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=2",
            "000000000000000000000000000000000000000000000000",
            "EFA71F788965BD4453F860178FC19101",
            "88B2B2706B105E36B446BB6D731A1E88");

        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=3",
            "EFA71F788965BD4453F860178FC191010000000000000000",
            "88B2B2706B105E36B446BB6D731A1E88",
            "39DA69D6BA4997D585B6DC073CA341B2");

        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=4",
            "88B2B2706B105E36B446BB6D731A1E88EFA71F788965BD44",
            "39DA69D6BA4997D585B6DC073CA341B2",
            "182B02D81497EA45F9DAACDC29193A65");

        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=5",
            "39DA69D6BA4997D585B6DC073CA341B288B2B2706B105E36",
            "182B02D81497EA45F9DAACDC29193A65",
            "7AFF7A70CA2FF28AC31DD8AE5DAAAB63");

        yield return CreateKnownAnswerTest(
            "Twofish-192 / I=10",
            "AE8109BFDA85C1F2C5038B34ED691BFF3AF6F7CE5BD35EF1",
            "893FD67B98C550073571BD631263FC78",
            "16434FC9C8841A63D58700B5578E8F67");
    }

    private static IEnumerable<KnownAnswerTest> GetKey256KnownAnswerTests()
    {
        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=1 / zero key, zero plaintext",
            "0000000000000000000000000000000000000000000000000000000000000000",
            "00000000000000000000000000000000",
            "57FF739D4DC92C1BD7FC01700CC8216F");

        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=2",
            "0000000000000000000000000000000000000000000000000000000000000000",
            "57FF739D4DC92C1BD7FC01700CC8216F",
            "D43BB7556EA32E46F2A282B7D45B4E0D");

        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=3",
            "57FF739D4DC92C1BD7FC01700CC8216F00000000000000000000000000000000",
            "D43BB7556EA32E46F2A282B7D45B4E0D",
            "90AFE91BB288544F2C32DC239B2635E6");

        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=4",
            "D43BB7556EA32E46F2A282B7D45B4E0D57FF739D4DC92C1BD7FC01700CC8216F",
            "90AFE91BB288544F2C32DC239B2635E6",
            "6CB4561C40BF0A9705931CB6D408E7FA");

        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=5",
            "90AFE91BB288544F2C32DC239B2635E6D43BB7556EA32E46F2A282B7D45B4E0D",
            "6CB4561C40BF0A9705931CB6D408E7FA",
            "3059D6D61753B958D92F4781C8640E58");

        yield return CreateKnownAnswerTest(
            "Twofish-256 / I=10",
            "DC096BCD99FC72F79936D4C748E75AF75AB67A5F8539A4A5FD9F0373BA463466",
            "C5A3E7CEE0F1B7260528A68FB4EA05F2",
            "43D5CEC327B24AB90AD34A79D0469151");
    }

    private static KnownAnswerTest CreateKnownAnswerTest(
        string name,
        string key,
        string input,
        string expectedOutput)
    {
        var keyBytes = Convert.FromHexString(key);

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
            CipherFactory = () => new TwofishBlockCipher(keyBytes),
        };
    }
}
