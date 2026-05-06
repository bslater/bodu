// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="SipHash{T}" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — disposed-state access, mid-stream property mutation, repeated
/// disposal, and boundary values for the round-count properties.
/// </summary>
public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that reading <see cref="SipHash{T}.AlgorithmName" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale or partially-zeroed string.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="SipHash{T}.CompressionRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the cleared (0) backing field.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CompressionRounds;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="SipHash{T}.FinalizationRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the cleared (0) backing field.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.FinalizationRounds;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.CompressionRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than silently mutating cleared state.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenSetAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.CompressionRounds = 4;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.FinalizationRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than silently mutating cleared state.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.FinalizationRounds = 8;
        });
    }

    /// <summary>
    /// Verifies that mutating <see cref="SipHash{T}.CompressionRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring the round count mid-computation.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[16];
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.CompressionRounds = 4;
        });
    }

    /// <summary>
    /// Verifies that mutating <see cref="SipHash{T}.FinalizationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring the round count mid-computation.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[16];
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.FinalizationRounds = 8;
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale digest or accessing
    /// the cleared key buffer.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHash(new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than touching the cleared key buffer or
    /// invoking <c>OnKeyChanged</c> against null state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Dispose()" /> twice on a SipHash instance is
    /// idempotent and does not throw.
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
            Assert.Fail($"Second Dispose on {typeof(TAlgorithm).Name} threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.CompressionRounds" /> to extreme values around
    /// the boundary throws <see cref="ArgumentOutOfRangeException" /> only for values strictly below
    /// <see cref="SipHash{T}.MinCompressionRounds" /> (and never some other unexpected exception).
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void CompressionRounds_WhenSetBelowMinimum_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.CompressionRounds = value;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.FinalizationRounds" /> to extreme values around
    /// the boundary throws <see cref="ArgumentOutOfRangeException" /> only for values strictly below
    /// <see cref="SipHash{T}.MinFinalizationRounds" /> (and never some other unexpected exception).
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void FinalizationRounds_WhenSetBelowMinimum_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.FinalizationRounds = value;
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.TransformFinalBlock" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than producing output from cleared state.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.TransformFinalBlock(new byte[1], 0, 1);
        });
    }
}
