// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a disposable holder for sensitive byte material that pins its buffer and zeroes it on disposal.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SecretBytes" /> is a hygiene measure for key material, passwords, and other secrets: the bytes are copied
/// into a buffer allocated on the pinned object heap (so the garbage collector cannot relocate it and leave stale
/// copies behind), the buffer is zeroed by <see cref="Clear" /> and <see cref="Dispose" /> using
/// <see cref="CryptographicOperations.ZeroMemory(Span{byte})" />, and <see cref="ToString" /> never reveals the
/// content.
/// </para>
/// <para>
/// Managed .NET cannot guarantee perfect secrecy: the source data existed in unprotected memory before it was copied
/// in, the runtime or a debugger may copy memory for diagnostics, and the operating system may page memory to disk.
/// Treat this type as defensive hygiene that narrows the exposure window — not as a secure enclave.
/// </para>
/// <para>
/// This type is not thread-safe; callers coordinate concurrent access and disposal.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Hold secret key material for the scope of an operation; the pinned buffer is zeroed on dispose.
/// using SecretBytes key = SecretBytes.Random(32);
///
/// // Hand a transient copy to a BCL API that requires a byte[], then erase that copy as well.
/// byte[] transient = key.ToArray();
/// try
/// {
///     using var hmac = new HMACSHA256(transient);
///     // ... use hmac ...
/// }
/// finally
/// {
///     CryptographicOperations.ZeroMemory(transient);
/// }
///]]>
/// </code>
/// </example>
public sealed class SecretBytes
    : IDisposable
{
    /// <summary>The pinned buffer holding the secret bytes. Zeroed by <see cref="Clear" /> and <see cref="Dispose" />.</summary>
    private readonly byte[] _buffer;

    /// <summary>Indicates whether <see cref="Dispose" /> has been called.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretBytes" /> class with a zero-filled pinned buffer of the
    /// specified length.
    /// </summary>
    /// <param name="length">The buffer length in bytes. Must be zero or greater.</param>
    private SecretBytes(int length)
    {
        _buffer = GC.AllocateArray<byte>(length, pinned: true);
    }

    /// <summary>
    /// Gets the number of bytes of secret material.
    /// </summary>
    /// <value>
    /// The buffer length in bytes. Remains readable after disposal, because the length of a secret is not itself
    /// secret.
    /// </value>
    public int Length =>
        _buffer.Length;

    /// <summary>
    /// Creates a <see cref="SecretBytes" /> instance filled with cryptographically secure random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>A new <see cref="SecretBytes" /> holding <paramref name="length" /> random bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> ≤ 0.</exception>
    public static SecretBytes Random(int length)
    {
        ThrowHelper.ThrowIfZeroOrNegative(length);

        var secret = new SecretBytes(length);
        CryptographyHelper.FillWithRandomBytes(secret._buffer);
        return secret;
    }

    /// <summary>
    /// Creates a <see cref="SecretBytes" /> instance containing a copy of the provided bytes.
    /// </summary>
    /// <param name="source">The bytes to copy. An empty span yields an empty instance.</param>
    /// <returns>A new <see cref="SecretBytes" /> holding a defensive copy of <paramref name="source" />.</returns>
    /// <remarks>
    /// The caller remains responsible for clearing <paramref name="source" /> if it holds the only other copy of the
    /// secret; this method cannot erase the original.
    /// </remarks>
    public static SecretBytes CopyFrom(ReadOnlySpan<byte> source)
    {
        var secret = new SecretBytes(source.Length);
        source.CopyTo(secret._buffer);
        return secret;
    }

    /// <summary>
    /// Returns a read-only view over the secret bytes.
    /// </summary>
    /// <returns>A read-only span over the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// The span aliases the live internal buffer: it observes <see cref="Clear" /> and must not be used after
    /// <see cref="Dispose" />.
    /// </remarks>
    public ReadOnlySpan<byte> AsSpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _buffer;
    }

    /// <summary>
    /// Copies the secret bytes into a new unprotected array.
    /// </summary>
    /// <returns>A new array containing the secret bytes.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// The returned array is an ordinary managed allocation outside this instance's control; callers should zero it
    /// (for example with <see cref="CryptographicOperations.ZeroMemory(Span{byte})" />) as soon as it is no longer
    /// needed.
    /// </remarks>
    public byte[] ToArray()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return (byte[])_buffer.Clone();
    }

    /// <summary>
    /// Compares this secret to another in fixed time.
    /// </summary>
    /// <param name="other">The secret to compare against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if both secrets have the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">
    /// This instance or <paramref name="other" /> has been disposed.
    /// </exception>
    public bool FixedTimeEquals(SecretBytes other)
    {
        ThrowHelper.ThrowIfNull(other);
        ObjectDisposedException.ThrowIf(_disposed, this);
        ObjectDisposedException.ThrowIf(other._disposed, other);

        return CryptographicOperations.FixedTimeEquals(_buffer, other._buffer);
    }

    /// <summary>
    /// Compares this secret to a raw byte sequence in fixed time.
    /// </summary>
    /// <param name="other">The bytes to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> has the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public bool FixedTimeEquals(ReadOnlySpan<byte> other)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return CryptographicOperations.FixedTimeEquals(_buffer, other);
    }

    /// <summary>
    /// Zeroes the secret bytes while leaving the instance usable.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// After clearing, the instance holds all-zero bytes of the original <see cref="Length" />.
    /// </remarks>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CryptographicOperations.ZeroMemory(_buffer);
    }

    /// <summary>
    /// Zeroes the secret bytes and marks the instance as disposed.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent. After disposal, <see cref="Length" /> and <see cref="ToString" /> remain usable; all
    /// members that reach the secret content throw <see cref="ObjectDisposedException" />.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographicOperations.ZeroMemory(_buffer);
        _disposed = true;
    }

    /// <summary>
    /// Returns a description of the instance that never includes the secret content.
    /// </summary>
    /// <returns>A string of the form <c>"SecretBytes (Length = N)"</c>.</returns>
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "SecretBytes (Length = {0})", _buffer.Length);
}
