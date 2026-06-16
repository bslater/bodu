// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.Ownership.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="GcmModeTransform" /> does not take ownership of the <see cref="IBlockCipher" />
/// passed to its constructor. The caller is responsible for the cipher's lifetime; disposing the GCM
/// transform must not dispose the underlying cipher.
/// </summary>
public sealed partial class GcmModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Dispose" /> does not call <see cref="IDisposable.Dispose" />
    /// on the inner <see cref="IBlockCipher" />. The class XML explicitly states ownership remains with the
    /// caller; this test pins that contract so a future refactor cannot silently flip it.
    /// </summary>
    [TestMethod]
    public void Dispose_ShouldNotDisposeInnerCipher()
    {
        var cipher = new DisposalTrackingBlockCipher();
        byte[] nonce = new byte[NonceSizeBytes];

        var transform = new GcmModeTransform(cipher, nonce);
        transform.Dispose();

        Assert.IsFalse(
            cipher.WasDisposed,
            "GcmModeTransform.Dispose must not propagate to the caller-owned IBlockCipher.");
    }

    /// <summary>
    /// Minimal <see cref="IBlockCipher" /> that records whether <see cref="IDisposable.Dispose" /> was called.
    /// Encrypt/Decrypt return a deterministic but arbitrary 16-byte output, enough to let
    /// <see cref="GcmModeTransform" /> construct successfully (it computes H and the keystream block during
    /// initialization). The output is not cryptographically meaningful — the test only inspects disposal.
    /// </summary>
    private sealed class DisposalTrackingBlockCipher
        : IBlockCipher
    {
        public int BlockSize => 128;

        public bool WasDisposed { get; private set; }

        public void Dispose() => WasDisposed = true;

        public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output) => input.CopyTo(output);

        public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output) => input.CopyTo(output);
    }
}
