// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.CreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Threefish.CreateDecryptor(byte[], byte[], byte[])" /> with a
    /// <see langword="null" /> tweak throws <see cref="ArgumentNullException" /> rather than
    /// <see cref="NullReferenceException" /> from an unguarded length read.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenTweakIsNull_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[32], new byte[32], null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateDecryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized tweak throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(32)]
    public void CreateDecryptor_WhenTweakLengthIsInvalid_ShouldThrowExactly(int tweakLength)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[32], new byte[32], new byte[tweakLength]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateDecryptor(byte[], byte[], byte[])" /> on a
    /// disposed instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WithTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[32], new byte[32], new byte[16]);
        });
    }
}
