// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemContractTests{T,T}.KeyReuse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class MLKemContractTests<TTest, TKem>
{
    /// <summary>
    /// Gets the engine parameters of the parameter set under test.
    /// </summary>
    /// <value>The <see cref="MLKemParameters" /> matching the specification's key-size designator.</value>
    private MLKemParameters EngineParameters =>
        GetSpecification().KeySizeDesignator switch
        {
            512 => MLKemParameters.MLKem512,
            768 => MLKemParameters.MLKem768,
            _ => MLKemParameters.MLKem1024,
        };

    /// <summary>
    /// Verifies that one key, used for many encapsulations and decapsulations through the values it caches, agrees
    /// with the engine deriving everything afresh from the encoded decapsulation key, for valid and tampered
    /// ciphertexts alike.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenKeyIsReusedAcrossManyCiphertexts_ShouldMatchTheUncachedEngine()
    {
        using var kem = new TKem();
        kem.GenerateKey();
        byte[] decapsulationKey = kem.ExportDecapsulationKey();
        byte[] uncached = new byte[MLKem.SharedSecretSizeInBytes];

        for (int iteration = 0; iteration < 32; iteration++)
        {
            (byte[] ciphertext, byte[] sharedSecret) = kem.Encapsulate();

            MLKemEngine.Decapsulate(EngineParameters, decapsulationKey, ciphertext, uncached);
            CollectionAssert.AreEqual(sharedSecret, kem.Decapsulate(ciphertext), $"iteration {iteration}: cached");
            CollectionAssert.AreEqual(sharedSecret, uncached, $"iteration {iteration}: uncached");

            ciphertext[iteration % ciphertext.Length] ^= 0x40;
            MLKemEngine.Decapsulate(EngineParameters, decapsulationKey, ciphertext, uncached);
            CollectionAssert.AreEqual(uncached, kem.Decapsulate(ciphertext), $"iteration {iteration}: tampered");
        }
    }

    /// <summary>
    /// Verifies that replacing an instance's encapsulation key replaces the values it caches: encapsulation afterwards
    /// produces a ciphertext that the new key's owner, and only the new key's owner, decapsulates to the same secret.
    /// </summary>
    [TestMethod]
    public void Encapsulate_WhenEncapsulationKeyIsReplaced_ShouldEncapsulateToTheNewKey()
    {
        using var first = new TKem();
        using var second = new TKem();
        using var encapsulator = new TKem();
        first.GenerateKey();
        second.GenerateKey();

        encapsulator.ImportEncapsulationKey(first.ExportEncapsulationKey());
        _ = encapsulator.Encapsulate();
        encapsulator.ImportEncapsulationKey(second.ExportEncapsulationKey());
        (byte[] ciphertext, byte[] sharedSecret) = encapsulator.Encapsulate();

        CollectionAssert.AreEqual(sharedSecret, second.Decapsulate(ciphertext));
        CollectionAssert.AreNotEqual(sharedSecret, first.Decapsulate(ciphertext));
    }

    /// <summary>
    /// Verifies that a key imported as an encoded decapsulation key, whose cached values are derived at import rather
    /// than handed over by key generation, decapsulates as the key pair it was exported from.
    /// </summary>
    [TestMethod]
    public void Decapsulate_WhenKeyWasImported_ShouldMatchTheGeneratedKey()
    {
        using var generated = new TKem();
        using var imported = new TKem();
        generated.GenerateKey();
        imported.ImportDecapsulationKey(generated.ExportDecapsulationKey());

        for (int iteration = 0; iteration < 8; iteration++)
        {
            (byte[] ciphertext, byte[] sharedSecret) = generated.Encapsulate();

            CollectionAssert.AreEqual(sharedSecret, imported.Decapsulate(ciphertext), $"iteration {iteration}");
        }
    }
}
