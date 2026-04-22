// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SDBM.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using the SDBM algorithm popularised by the public-domain NDBM database library. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, the running hash is updated as <c><![CDATA[hash = byte + (hash << 6) + (hash << 16) - hash]]></c>, producing
/// good distribution for short and medium-length keys at minimal cost.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class SDBM
    : System.Security.Cryptography.HashAlgorithm
{
    private bool disposed = false;
    private uint workingHash;
#if !NET6_0_OR_GREATER

    // Required for .NET Standard 2.0 or older frameworks
    private bool finalized;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="SDBM" /> class.
    /// </summary>
    public SDBM()
    {
        this.HashSizeValue = 32;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        State = 0;
        finalized = false;
#endif
        this.workingHash = 0;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
    /// </param>
    /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (this.disposed) return;
        if (disposing)
        {
            CryptoHelpers.ClearAndNullify(ref this.HashValue);

            this.workingHash = 0;
            this.HashSizeValue = 0;
        }

        this.disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Processes a segment of the input byte array and feeds it into the <see cref="SDBM" /> hashing algorithm. This method updates the
    /// internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array" /> at which to begin reading data.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="ibStart" /> is less than 0.</para>
    /// <para>-or-</para>
    /// <para><paramref name="cbSize" /> is less than 0.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart" /> and <paramref name="cbSize" /> specify a range that exceeds the length of <paramref name="array" />.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        ThrowHelper.ThrowIfLessThan(ibStart, 0);
        ThrowHelper.ThrowIfLessThan(cbSize, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        this.HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="SDBM" /> hashing algorithm. This
    /// method updates the internal hash state accordingly by consuming the entire input span.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif
        var v = this.workingHash;
        foreach (var b in source)
        {
            v = b + (v << 6) + (v << 16) - v;
        }

        this.workingHash = v;
    }

    /// <summary>
    /// Finalises the SDBM hash computation and returns the 32-bit result as a 4-byte big-endian array.
    /// </summary>
    /// <returns>A 4-byte array containing the hash value in <b>big-endian</b> byte order.</returns>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
        finalized = true;
        State = 2;
#endif
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(span, this.workingHash);
        return span.ToArray();
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this.disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(SDBM));
#endif
    }

    /// <summary>
    /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already started processing data,
    /// indicating that the instance is in a finalized or non-configurable state.
    /// </summary>
    /// <remarks>
    /// This method is used to prevent reconfiguration of algorithm parameters such as the key, number of rounds, or other settings once
    /// hashing has begun. It ensures settings are immutable after initialization.
    /// </remarks>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
    /// started or been finalized.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
