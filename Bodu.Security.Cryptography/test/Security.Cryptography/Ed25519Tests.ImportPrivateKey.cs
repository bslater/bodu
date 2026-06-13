// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.ImportPrivateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="Ed25519.ImportPrivateKey" /> and <see cref="Ed25519.GenerateKey" />.
/// </summary>
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPrivateKey" /> derives the public keys published in the RFC 8032 §7.1
    /// test vectors.
    /// </summary>
    [TestMethod]
    [DataRow(
        "RFC 8032 TEST 1",
        "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60",
        "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a")]
    [DataRow(
        "RFC 8032 TEST 2",
        "4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb",
        "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c")]
    [DataRow(
        "RFC 8032 TEST 3",
        "c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7",
        "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025")]
    [DataRow(
        "RFC 8032 TEST SHA(abc)",
        "833fe62409237b9d62ec77587520911e9a759cec1d19755b7da901b96dca3d42",
        "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf")]
    public void ImportPrivateKey_WhenGivenRfc8032Seed_ShouldDerivePublishedPublicKey(
        string testName, string seedHex, string expectedPublicHex)
    {
        _ = testName;

        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString(seedHex));

        Assert.IsTrue(algorithm.HasPrivateKey);
        CollectionAssert.AreEqual(Convert.FromHexString(expectedPublicHex), algorithm.ExportPublicKey());
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPrivateKey" /> throws <see cref="ArgumentException" /> for seed
    /// material that is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    [DataRow("31 bytes", 31)]
    [DataRow("33 bytes", 33)]
    [DataRow("empty", 0)]
    public void ImportPrivateKey_WhenLengthIsInvalid_ShouldThrowArgumentException(string testName, int length)
    {
        _ = testName;

        using var algorithm = new Ed25519();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.ImportPrivateKey(new byte[length]);
        });

        Assert.AreEqual("privateKey", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.GenerateKey" /> populates both keys and that consecutive generations differ.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalledTwice_ShouldPopulateKeysAndDiffer()
    {
        using var algorithm = new Ed25519();

        algorithm.GenerateKey();
        Assert.IsTrue(algorithm.HasPrivateKey);
        Assert.IsTrue(algorithm.HasPublicKey);
        var first = algorithm.ExportPrivateKey();

        algorithm.GenerateKey();
        CollectionAssert.AreNotEqual(first, algorithm.ExportPrivateKey());
    }
}
