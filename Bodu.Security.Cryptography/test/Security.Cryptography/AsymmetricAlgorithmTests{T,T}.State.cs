// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.State.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that re-assigning the single legal key size leaves the existing key material untouched on generated,
    /// public-only, and keyless instances (the parameter set is fixed, so the assignment is a no-op for key state).
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedAcrossKeyStates_ShouldPreserveKeyMaterial()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();

        using (TAlgorithm generated = CreateAlgorithmWithGeneratedKey())
        {
            byte[] publicKey = ExportPublicKey(generated);
            byte[] privateKey = ExportPrivateKey(generated);

            generated.KeySize = spec.KeySizeDesignator;

            Assert.IsTrue(HasPrivateKey(generated));
            Assert.IsTrue(HasPublicKey(generated));
            CollectionAssert.AreEqual(publicKey, ExportPublicKey(generated));
            CollectionAssert.AreEqual(privateKey, ExportPrivateKey(generated));
        }

        using (TAlgorithm donor = CreateAlgorithmWithGeneratedKey())
        using (TAlgorithm publicOnly = CreatePublicOnlyAlgorithm(donor))
        {
            byte[] publicKey = ExportPublicKey(publicOnly);

            publicOnly.KeySize = spec.KeySizeDesignator;

            Assert.IsFalse(HasPrivateKey(publicOnly));
            Assert.IsTrue(HasPublicKey(publicOnly));
            CollectionAssert.AreEqual(publicKey, ExportPublicKey(publicOnly));
        }

        using (TAlgorithm keyless = CreateAlgorithm())
        {
            keyless.KeySize = spec.KeySizeDesignator;

            Assert.IsFalse(HasPrivateKey(keyless));
            Assert.IsFalse(HasPublicKey(keyless));
        }
    }

    /// <summary>
    /// Verifies that the export adapters return independent copies, so mutating a returned array does not affect the
    /// instance's stored key or any subsequent export.
    /// </summary>
    [TestMethod]
    public void ExportKey_WhenReturnedArrayMutated_ShouldNotAffectStoredKey()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        byte[] firstPublic = ExportPublicKey(algorithm);
        byte[] referencePublic = (byte[])firstPublic.Clone();
        firstPublic[0] ^= 0xFF;
        CollectionAssert.AreEqual(referencePublic, ExportPublicKey(algorithm));

        byte[] firstPrivate = ExportPrivateKey(algorithm);
        byte[] referencePrivate = (byte[])firstPrivate.Clone();
        firstPrivate[0] ^= 0xFF;
        CollectionAssert.AreEqual(referencePrivate, ExportPrivateKey(algorithm));
    }

    /// <summary>
    /// Verifies the import trust-boundary gate: the raw public-key and private-key import adapters reject empty and
    /// clearly-too-short inputs with <see cref="ArgumentException" />. This is the structural floor every asymmetric
    /// algorithm must satisfy; algorithm-specific canonical and consistency checks are tested per type.
    /// </summary>
    [TestMethod]
    public void ImportMembers_WhenGivenMalformedInput_ShouldReject()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        // Every asymmetric key in scope is at least 32 bytes, so 0/1/3-byte inputs are invalid for all of them.
        foreach (byte[] malformed in new[] { Array.Empty<byte>(), new byte[1], new byte[3] })
        {
            Assert.ThrowsExactly<ArgumentException>(() => ImportPublicKey(algorithm, malformed));
            Assert.ThrowsExactly<ArgumentException>(() => ImportPrivateKey(algorithm, malformed));
        }
    }
}
