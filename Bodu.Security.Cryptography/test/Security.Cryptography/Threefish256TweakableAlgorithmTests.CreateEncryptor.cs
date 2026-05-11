// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// <see langword="null" /> tweak throws <see cref="ArgumentNullException" /> rather than
    /// <see cref="NullReferenceException" /> from an unguarded length read.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenTweakIsNull_ShouldThrowExactly()
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowExactly()
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(null!, new byte[32], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// <see langword="null" /> IV throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIVIsNull_ShouldThrowExactly()
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], null!, new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized tweak throws <see cref="CryptographicException" /> rather than producing a
    /// transform with truncated or padded tweak material.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(32)]
    public void CreateEncryptor_WhenTweakLengthIsInvalid_ShouldThrowExactly(int tweakLength)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], new byte[tweakLength]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized key throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(64)]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[keyLength], new byte[32], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized IV throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(64)]
    public void CreateEncryptor_WhenIVLengthIsInvalid_ShouldThrowExactly(int ivLength)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[ivLength], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> on a
    /// disposed instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WithTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        Threefish256 algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], new byte[16]);
        });
    }
}
