// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.ProtectedHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the protected helpers declared on <see cref="TweakableSymmetricAlgorithm" /> —
/// <see cref="TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize(byte[])" />,
/// <see cref="TweakableSymmetricAlgorithm.ThrowIfTweakNotSet" />, and the
/// <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> getter when the backing field is uninitialised.
/// </summary>
[TestClass]
public sealed class TweakableSymmetricAlgorithmProtectedHelperTests
{
    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> returns an empty array when
    /// the backing <c>LegalTweakSizesValue</c> field is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void LegalTweakSizes_WhenBackingFieldIsNull_ShouldReturnEmptyArray()
    {
        using var algorithm = new BareTweakableAlgorithm();

        var result = algorithm.LegalTweakSizes;

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> returns a cloned copy of the
    /// underlying array when <c>LegalTweakSizesValue</c> is populated, isolating callers from internal state.
    /// </summary>
    [TestMethod]
    public void LegalTweakSizes_WhenBackingFieldHasValues_ShouldReturnDefensiveClone()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);

        var first = algorithm.LegalTweakSizes;
        var second = algorithm.LegalTweakSizes;

        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, first.Length);
        Assert.AreEqual(128, first[0].MinSize);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize(byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the supplied tweak is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidTweakSize_ByteArray_WhenNull_ShouldThrowExactly()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.PublicThrowIfInvalidTweakSize((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize(byte[])" /> does not throw
    /// when the tweak length, in bits, matches a legal size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidTweakSize_ByteArray_WhenSizeIsLegal_ShouldNotThrow()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);

        algorithm.PublicThrowIfInvalidTweakSize(new byte[16]);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize(byte[])" /> throws
    /// <see cref="CryptographicException" /> when the tweak length, in bits, is not a legal size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidTweakSize_ByteArray_WhenSizeIsIllegal_ShouldThrowExactly()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.PublicThrowIfInvalidTweakSize(new byte[7]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfTweakNotSet" /> throws
    /// <see cref="CryptographicException" /> when no tweak value has been assigned.
    /// </summary>
    [TestMethod]
    public void ThrowIfTweakNotSet_WhenTweakIsNull_ShouldThrowExactly()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.PublicThrowIfTweakNotSet();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfTweakNotSet" /> throws
    /// <see cref="CryptographicException" /> when the assigned tweak is an empty array.
    /// </summary>
    [TestMethod]
    public void ThrowIfTweakNotSet_WhenTweakIsEmpty_ShouldThrowExactly()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);
        algorithm.AssignTweak(Array.Empty<byte>());

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.PublicThrowIfTweakNotSet();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.ThrowIfTweakNotSet" /> does not throw when a
    /// non-empty tweak value has been assigned.
    /// </summary>
    [TestMethod]
    public void ThrowIfTweakNotSet_WhenTweakIsSet_ShouldNotThrow()
    {
        using var algorithm = new BareTweakableAlgorithm(legalTweakSizes:
        [
            new KeySizes(128, 128, 0),
        ]);
        algorithm.AssignTweak(new byte[16]);

        algorithm.PublicThrowIfTweakNotSet();
    }

    /// <summary>
    /// Minimal <see cref="TweakableSymmetricAlgorithm" /> stub used to exercise protected helpers.
    /// </summary>
    private sealed class BareTweakableAlgorithm : TweakableSymmetricAlgorithm
    {
        /// <summary>
        /// Initialises a new instance with no legal tweak sizes by default.
        /// </summary>
        /// <param name="legalTweakSizes">An optional set of legal tweak sizes. When omitted, the backing field remains <see langword="null" />.</param>
        public BareTweakableAlgorithm(KeySizes[]? legalTweakSizes = null)
        {
            this.LegalKeySizesValue = [new KeySizes(8, 2048, 8)];
            this.LegalBlockSizesValue = [new KeySizes(64, 64, 0)];
            this.KeySizeValue = 64;
            this.BlockSizeValue = 64;
            if (legalTweakSizes is not null)
                this.LegalTweakSizesValue = legalTweakSizes;
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void GenerateIV() => this.IVValue = new byte[this.BlockSize / 8];

        /// <inheritdoc />
        public override void GenerateKey() => this.KeyValue = new byte[this.KeySize / 8];

        /// <inheritdoc />
        public override void GenerateTweak()
        {
            if (this.LegalTweakSizesValue is null || this.LegalTweakSizesValue.Length == 0)
                throw new CryptographicException();
            this.TweakValue = new byte[this.LegalTweakSizesValue[0].MinSize / 8];
        }

        /// <summary>Assigns the underlying tweak buffer directly, without size validation.</summary>
        /// <param name="value">The tweak value to assign.</param>
        public void AssignTweak(byte[] value) => this.TweakValue = value;

        /// <summary>Exposes the protected <see cref="TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize(byte[])" /> helper.</summary>
        /// <param name="tweak">The tweak value to validate.</param>
        public void PublicThrowIfInvalidTweakSize(byte[] tweak) => this.ThrowIfInvalidTweakSize(tweak);

        /// <summary>Exposes the protected <see cref="TweakableSymmetricAlgorithm.ThrowIfTweakNotSet" /> helper.</summary>
        public void PublicThrowIfTweakNotSet() => this.ThrowIfTweakNotSet();
    }
}
