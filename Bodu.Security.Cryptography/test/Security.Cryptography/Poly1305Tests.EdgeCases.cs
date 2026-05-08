// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Poly1305" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — its one-time-use contract, idempotent disposal, padding contract,
/// and post-finalisation reuse semantics.
/// </summary>
public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that <see cref="Poly1305.CanReuseTransform" /> is <see langword="false" /> as
    /// documented in RFC 8439, signalling to consumers that the algorithm must not be reused
    /// across multiple messages with the same key.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_ShouldBeFalse()
    {
        using var poly = new Poly1305();

        Assert.IsFalse(poly.CanReuseTransform,
            "Poly1305 is a one-time authenticator and must report CanReuseTransform = false.");
    }

    /// <summary>
    /// Verifies that <see cref="Poly1305.CanTransformMultipleBlocks" /> is <see langword="true" />
    /// so the framework streams data through the algorithm in block-sized chunks.
    /// </summary>
    [TestMethod]
    public void CanTransformMultipleBlocks_ShouldBeTrue()
    {
        using var poly = new Poly1305();

        Assert.IsTrue(poly.CanTransformMultipleBlocks);
    }

    /// <summary>
    /// Verifies that calling <see cref="Poly1305.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var poly = new Poly1305();
        poly.Dispose();

        try
        {
            poly.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Poly1305 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Poly1305" /> instance — one whose
    /// key was set by the constructor but never used — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var poly = new Poly1305();

        try
        {
            poly.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Poly1305 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed
    /// <see cref="Poly1305" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// producing output from cleared key material.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var poly = new Poly1305();
        poly.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = poly.ComputeHash(new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="Poly1305" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// invoking <c>OnKeyChanged</c> against cleared state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var poly = new Poly1305();
        poly.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            poly.Initialize();
        });
    }
}
