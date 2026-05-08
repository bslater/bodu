// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="AsconXof128" /> for unexpected exceptions when its public surface is
/// exercised outside of expected usage — disposed-state access on properties and lifecycle
/// methods, idempotent disposal, and the static <c>HashData</c> overloads.
/// </summary>
public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that calling <see cref="AsconXof128.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on AsconXof128 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="AsconXof128" /> instance — one
    /// that has never been absorbed, squeezed, or had any property accessed — completes without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var sut = new AsconXof128();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched AsconXof128 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconXof{T}.Initialize" /> on a disposed
    /// <see cref="AsconXof128" /> instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Initialize();
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="AsconXof{T}.AlgorithmName" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.GetHash" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than producing output from cleared state.
    /// </summary>
    [TestMethod]
    public void GetHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.GetHash(32);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.HashData(ReadOnlySpan{byte}, int)" /> with
    /// <c>outputLength = 0</c> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void HashData_WhenOutputLengthIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = AsconXof128.HashData([0x01], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.HashData(ReadOnlySpan{byte}, int)" /> with a negative
    /// <c>outputLength</c> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void HashData_WhenOutputLengthIsNegative_ShouldThrowExactly(int outputLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = AsconXof128.HashData([0x01], outputLength);
        });
    }
}
