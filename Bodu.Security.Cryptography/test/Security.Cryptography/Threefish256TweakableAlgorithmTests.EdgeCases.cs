// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Threefish256" /> for unexpected exceptions when its public surface is
/// exercised outside of expected usage — null tweak/key/IV, wrong-sized arrays, disposed-state
/// access, and idempotent disposal. Complements the generic
/// <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base coverage.
/// </summary>
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
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], null!);
        });
    }

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
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();

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
        using var algorithm = CreateAlgorithm();

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
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], new byte[tweakLength]);
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
        using var algorithm = CreateAlgorithm();

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
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[ivLength], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.GenerateTweak" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.GenerateTweak();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> on a
    /// disposed instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WithTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[32], new byte[16]);
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

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Threefish256" /> instance — one
    /// that has never had its key, IV, tweak, or any other property accessed — completes without
    /// throwing. Regression guard for disposal paths that touch lazily-initialised state without
    /// null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Threefish256 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="Threefish.Dispose" /> twice is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Threefish256 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that the algorithm-specific <see cref="Threefish.BlockMode" /> property accepts
    /// every <see cref="CipherBlockMode" /> value without throwing — it is a plain auto-property
    /// and must not gain accidental enum validation.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.ECB)]
    [DataRow(CipherBlockMode.CBC)]
    [DataRow(CipherBlockMode.CFB)]
    [DataRow(CipherBlockMode.OFB)]
    [DataRow(CipherBlockMode.CTR)]
    public void BlockMode_WhenSetToValidValue_ShouldNotThrow(CipherBlockMode mode)
    {
        using var algorithm = CreateAlgorithm();

        try
        {
            algorithm.BlockMode = mode;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting Threefish256.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that assigning <see cref="TweakableSymmetricAlgorithm.TweakSize" /> to <c>0</c>
    /// throws <see cref="CryptographicException" /> rather than leaving the algorithm in an
    /// invalid configuration where <see cref="TweakableSymmetricAlgorithm.Tweak" /> would later
    /// fail with <see cref="NullReferenceException" /> or another unexpected exception.
    /// </summary>
    [TestMethod]
    public void TweakSize_WhenSetToZero_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = 0;
        });
    }

    /// <summary>
    /// Verifies that assigning a wrongly-sized <see cref="TweakableSymmetricAlgorithm.TweakSize" />
    /// throws <see cref="CryptographicException" /> for a representative range of off-by-one and
    /// off-by-many values.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(64)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(256)]
    [DataRow(-1)]
    public void TweakSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = value;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> on a
    /// disposed instance does not throw <see cref="NullReferenceException" /> from a cleared
    /// backing field.
    /// </summary>
    [TestMethod]
    public void LegalTweakSizes_WhenAccessedAfterDispose_ShouldNotThrowUnexpected()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            var sizes = algorithm.LegalTweakSizes;
            Assert.IsNotNull(sizes);
        }
        catch (ObjectDisposedException)
        {
            // Acceptable — disposal contract may forbid further reads.
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            Assert.Fail(
                $"Reading LegalTweakSizes after Dispose threw an unexpected exception: " +
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
