// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests.ImportExport.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that an exported private key round-trips through the private-key import, restoring both the private
    /// material and the matching public key.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenRoundTrippingExportedKey_ShouldRestoreBothKeys()
    {
        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();

        using TAlgorithm restored = CreateAlgorithm();
        ImportPrivateKey(restored, ExportPrivateKey(donor));

        Assert.IsTrue(HasPrivateKey(restored));
        Assert.IsTrue(HasPublicKey(restored));
        CollectionAssert.AreEqual(ExportPrivateKey(donor), ExportPrivateKey(restored));
        CollectionAssert.AreEqual(ExportPublicKey(donor), ExportPublicKey(restored));
    }

    /// <summary>
    /// Verifies that importing a public key produces a public-only instance, discarding any private key previously
    /// held.
    /// </summary>
    [TestMethod]
    public void ImportPublicKey_WhenCalled_ShouldCreatePublicOnlyInstanceAndDiscardPrivateKey()
    {
        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        var publicKey = ExportPublicKey(donor);

        using TAlgorithm publicOnly = CreateAlgorithm();
        ImportPublicKey(publicOnly, publicKey);
        Assert.IsFalse(HasPrivateKey(publicOnly));
        Assert.IsTrue(HasPublicKey(publicOnly));
        CollectionAssert.AreEqual(publicKey, ExportPublicKey(publicOnly));

        using TAlgorithm replaced = CreateAlgorithmWithGeneratedKey();
        ImportPublicKey(replaced, publicKey);
        Assert.IsFalse(HasPrivateKey(replaced));
    }

    /// <summary>
    /// Verifies that importing key material of any length other than the specification's exact sizes throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ImportKeys_WhenLengthIsInvalid_ShouldThrowArgumentException()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithm();

        foreach (var length in new[] { 0, spec.PrivateKeySizeBytes - 1, spec.PrivateKeySizeBytes + 1 })
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => { ImportPrivateKey(algorithm, new byte[length]); },
                $"ImportPrivateKey must reject {length} bytes.");
        }

        foreach (var length in new[] { 0, spec.PublicKeySizeBytes - 1, spec.PublicKeySizeBytes + 1 })
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => { ImportPublicKey(algorithm, new byte[length]); },
                $"ImportPublicKey must reject {length} bytes.");
        }
    }

    /// <summary>
    /// Verifies that exporting missing key material throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void ExportKeys_WhenNoKeyMaterialPresent_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() => { _ = ExportPrivateKey(algorithm); });
        Assert.ThrowsExactly<CryptographicException>(() => { _ = ExportPublicKey(algorithm); });
    }

    /// <summary>
    /// Verifies that the arrays returned by the export adapters are defensive copies whose mutation does not affect
    /// later exports.
    /// </summary>
    [TestMethod]
    public void ExportKeys_WhenReturnedArrayIsMutated_ShouldNotAffectStoredKeys()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        var privateKey = ExportPrivateKey(algorithm);
        var privateOriginal = (byte[])privateKey.Clone();
        privateKey[0] ^= 0xFF;
        CollectionAssert.AreEqual(privateOriginal, ExportPrivateKey(algorithm));

        var publicKey = ExportPublicKey(algorithm);
        var publicOriginal = (byte[])publicKey.Clone();
        publicKey[0] ^= 0xFF;
        CollectionAssert.AreEqual(publicOriginal, ExportPublicKey(algorithm));
    }
}
