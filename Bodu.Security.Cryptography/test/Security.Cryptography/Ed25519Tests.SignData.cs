// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.SignData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="Ed25519.SignData(ReadOnlySpan{byte})" /> and its span-writing overload.
/// </summary>
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that signing is deterministic: the same message under the same key yields the identical signature.
    /// </summary>
    [TestMethod]
    public void SignData_WhenSigningSameMessageTwice_ShouldProduceIdenticalSignatures()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();
        var message = new byte[] { 1, 2, 3, 4, 5 };

        CollectionAssert.AreEqual(algorithm.SignData(message), algorithm.SignData(message));
    }

    /// <summary>
    /// Verifies that the span-writing overload produces the same signature as the allocating overload.
    /// </summary>
    [TestMethod]
    public void SignData_WhenUsingSpanOverload_ShouldMatchAllocatingOverload()
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
        var message = new byte[] { 0x72 };

        var allocating = algorithm.SignData(message);
        var spanResult = new byte[Ed25519.SignatureSizeInBytes];
        algorithm.SignData(message, spanResult);

        CollectionAssert.AreEqual(allocating, spanResult);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.SignData(ReadOnlySpan{byte})" /> throws
    /// <see cref="CryptographicException" /> when no private key is present.
    /// </summary>
    [TestMethod]
    public void SignData_WhenNoPrivateKeyPresent_ShouldThrowCryptographicException()
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPublicKey(Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a"));

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.SignData(ReadOnlySpan<byte>.Empty);
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that the span overload throws <see cref="ArgumentException" /> when the destination is not exactly
    /// 64 bytes.
    /// </summary>
    [TestMethod]
    public void SignData_WhenDestinationLengthIsInvalid_ShouldThrowArgumentException()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.SignData(new byte[] { 1 }, new byte[63]);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }
}
