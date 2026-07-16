// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RawKeyAsymmetricAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the base class for the library's raw-key asymmetric algorithms (X25519, Ed25519, ML-KEM, ML-DSA),
/// centralizing the shared key-material lifecycle: ownership of the current <see cref="AsymmetricKeyMaterial" />,
/// zeroizing replacement, dispose-time clearing, and the disposed-state guard.
/// </summary>
/// <remarks>
/// <para>
/// Derived algorithms store their current key pair (or public-only key) through
/// <see cref="ReplaceKeyMaterial(AsymmetricKeyMaterial)" />, which zeroizes any previously held private key before
/// taking ownership of the new material, and read it back through <see cref="KeyMaterial" />. Disposal clears the held
/// private key exactly once and latches the instance; subsequent operations guard with <see cref="ThrowIfDisposed" />.
/// </para>
/// <para>
/// The lifecycle members are <c>private protected</c>: they form the internal contract between this base and the
/// library's own algorithm implementations, not a public extension surface.
/// </para>
/// </remarks>
public abstract class RawKeyAsymmetricAlgorithm
    : AsymmetricAlgorithm
{
    /// <summary>The current key material, or <see langword="null" /> when no key has been generated or imported.</summary>
    private AsymmetricKeyMaterial? _keyMaterial;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawKeyAsymmetricAlgorithm" /> class.
    /// </summary>
    private protected RawKeyAsymmetricAlgorithm()
    {
    }

    /// <summary>
    /// Gets the current key material, or <see langword="null" /> when no key has been generated or imported.
    /// </summary>
    /// <value>The key material owned by this instance.</value>
    private protected AsymmetricKeyMaterial? KeyMaterial =>
        _keyMaterial;

    /// <summary>
    /// Releases the resources used by this instance, zeroing all private key material.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        if (disposing)
        {
            _keyMaterial?.Clear();
            _keyMaterial = null;
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Replaces the instance's key material, zeroizing any previously held private key first.
    /// </summary>
    /// <param name="keyMaterial">The new key material to take ownership of.</param>
    private protected void ReplaceKeyMaterial(AsymmetricKeyMaterial keyMaterial)
    {
        _keyMaterial?.Clear();
        _keyMaterial = keyMaterial;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> when the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    private protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
