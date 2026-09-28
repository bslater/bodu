// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaContractTests{T,T}.KeyReuse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class MLDsaContractTests<TTest, TDsa>
{
    /// <summary>
    /// Gets the engine parameters of the parameter set under test.
    /// </summary>
    /// <value>The <see cref="MLDsaParameters" /> matching the specification's key-size designator.</value>
    private MLDsaParameters EngineParameters =>
        GetSpecification().KeySizeDesignator switch
        {
            44 => MLDsaParameters.MLDsa44,
            65 => MLDsaParameters.MLDsa65,
            _ => MLDsaParameters.MLDsa87,
        };

    /// <summary>
    /// Verifies that one key, signing many messages deterministically through the values it caches, produces the
    /// signatures the engine produces when it derives everything afresh from the encoded private key.
    /// </summary>
    [TestMethod]
    public void SignData_WhenKeyIsReusedAcrossManyMessages_ShouldMatchTheUncachedEngine()
    {
        using var dsa = new TDsa { DeterministicSigning = true };
        dsa.GenerateKey();
        byte[] privateKey = dsa.ExportPrivateKey();
        byte[] context = [0x01, 0x02, 0x03];
        byte[] uncached = new byte[dsa.SignatureSizeInBytes];
        var random = new Random(0x0204_0500);

        for (int iteration = 0; iteration < 16; iteration++)
        {
            byte[] message = new byte[iteration * 7];
            random.NextBytes(message);

            MLDsaEngine.Sign(EngineParameters, privateKey, context, message, new byte[32], uncached);

            CollectionAssert.AreEqual(uncached, dsa.SignData(message, context), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that one public key, verifying many signatures through the values it caches, reaches the verdict the
    /// engine reaches when it derives everything afresh from the encoded public key, for valid and tampered signatures
    /// alike.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenKeyIsReusedAcrossManySignatures_ShouldMatchTheUncachedEngine()
    {
        using var signer = new TDsa();
        using var verifier = new TDsa();
        signer.GenerateKey();
        byte[] publicKey = signer.ExportPublicKey();
        verifier.ImportPublicKey(publicKey);
        var random = new Random(0x0204_0501);

        for (int iteration = 0; iteration < 16; iteration++)
        {
            byte[] message = new byte[iteration * 7];
            random.NextBytes(message);
            byte[] signature = signer.SignData(message);
            if (iteration % 2 == 1)
                signature[random.Next(signature.Length)] ^= (byte)(1 << random.Next(8));

            bool expected = MLDsaEngine.Verify(EngineParameters, publicKey, default, message, signature);

            Assert.AreEqual(expected, verifier.VerifyData(message, signature), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that a key imported as an encoded private key, whose cached values import computes rather than key
    /// generation, signs deterministically as the key pair it was exported from.
    /// </summary>
    [TestMethod]
    public void SignData_WhenKeyWasImported_ShouldMatchTheGeneratedKey()
    {
        using var generated = new TDsa { DeterministicSigning = true };
        using var imported = new TDsa { DeterministicSigning = true };
        generated.GenerateKey();
        imported.ImportPrivateKey(generated.ExportPrivateKey());

        for (int iteration = 0; iteration < 8; iteration++)
        {
            byte[] message = [(byte)iteration, 0x5A];

            CollectionAssert.AreEqual(generated.SignData(message), imported.SignData(message), $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that replacing an instance's private key replaces the values it caches: signatures made afterwards
    /// verify under the new key's public key and not under the old one's.
    /// </summary>
    [TestMethod]
    public void SignData_WhenPrivateKeyIsReplaced_ShouldSignWithTheNewKey()
    {
        using var first = new TDsa();
        using var second = new TDsa();
        using var signer = new TDsa();
        first.GenerateKey();
        second.GenerateKey();
        byte[] message = [0x42];

        signer.ImportPrivateKey(first.ExportPrivateKey());
        _ = signer.SignData(message);
        signer.ImportPrivateKey(second.ExportPrivateKey());
        byte[] signature = signer.SignData(message);

        Assert.IsTrue(second.VerifyData(message, signature), "new key");
        Assert.IsFalse(first.VerifyData(message, signature), "old key");
    }

    /// <summary>
    /// Verifies that replacing an instance's public key replaces the values it caches: afterwards it accepts the new
    /// key's signatures and rejects the old key's.
    /// </summary>
    [TestMethod]
    public void VerifyData_WhenPublicKeyIsReplaced_ShouldVerifyAgainstTheNewKey()
    {
        using var first = new TDsa();
        using var second = new TDsa();
        using var verifier = new TDsa();
        first.GenerateKey();
        second.GenerateKey();
        byte[] message = [0x24];
        byte[] firstSignature = first.SignData(message);
        byte[] secondSignature = second.SignData(message);

        verifier.ImportPublicKey(first.ExportPublicKey());
        Assert.IsTrue(verifier.VerifyData(message, firstSignature), "first key");
        verifier.ImportPublicKey(second.ExportPublicKey());

        Assert.IsTrue(verifier.VerifyData(message, secondSignature), "new key");
        Assert.IsFalse(verifier.VerifyData(message, firstSignature), "old key");
    }
}
