// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 64-bit non-cryptographic hash using the ELF (Executable and Linkable Format) hash algorithm originally used in UNIX System
/// V object files. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ELF hashing shifts and folds the running hash for each byte of input, periodically XORing the high bits back into the low bits. An
/// optional <see cref="Seed" /> can be supplied to alter the initial state; it cannot be changed once hashing has begun.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Elf64
    : System.Security.Cryptography.HashAlgorithm
{
    private const ulong HighBitsMask = 0xF000000000000000UL;
    private const int HighBitsShift = 56;

    private bool _disposed = false;
    private ulong _seedValue;
    private ulong _workingHash;
#if !NET6_0_OR_GREATER

    // Required for .NET Standard 2.0 or older frameworks
    private bool _finalized;
#endif

    /// <summary>
    /// Initialises a new instance of the <see cref="Elf64" /> class.
    /// </summary>
    public Elf64()
    {
        this.HashSizeValue = 64;
        this.Initialize();
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets or sets the seed used to initialise the internal hash state.
    /// </summary>
    /// <value>The seed value applied before hashing begins.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    /// <remarks>
    /// Changing the seed influences the initial hash state and therefore the resulting hash output. Common seed values such as 31, 131,
    /// or 1313 are often used to reduce clustering or bias.
    /// </remarks>
    public ulong Seed
    {
        get
        {
            this.ThrowIfDisposed();

            return this._seedValue;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();

            this._seedValue = value;
            this.Initialize();
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        State = 0;
        finalized = false;
#endif
        this._workingHash = this._seedValue;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
    /// </param>
    /// <remarks>Ensures all internal state is overwritten with zeros before releasing resources.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.ClearAndNullify(ref this.HashValue);

            this._seedValue = 0;
            this._workingHash = 0;
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Processes a segment of the input byte array and feeds it into the <see cref="Elf64" /> hashing algorithm. This method updates
    /// the internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
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
    /// The hash algorithm has already been finalised and cannot accept more input data.
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
    /// Processes the input <paramref name="source" /> and feeds it into the <see cref="Elf64" /> hashing algorithm, updating the
    /// internal hash state accordingly.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalised and cannot accept more input data.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif
        ulong v = this._workingHash;

        foreach (byte b in source)
        {
            v = (v << 4) + b;

            ulong high = v & HighBitsMask;
            v ^= high >> HighBitsShift;
            v &= ~high;
        }

        this._workingHash = v;
    }

    /// <summary>
    /// Finalises the ELF-64 hash computation and returns the 64-bit result as an 8-byte big-endian array.
    /// </summary>
    /// <returns>An 8-byte array containing the hash value in <b>big-endian</b> byte order.</returns>
    /// <exception cref="CryptographicUnexpectedOperationException">Thrown when the hash algorithm has been disposed or has produced an unexpected finalisation state.</exception>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

        finalized = true;
        State = 2;
#endif

        return this._workingHash.GetBytes(asBigEndian: true);
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
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(Elf64));
#endif
    }

    /// <summary>
    /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already started processing data,
    /// indicating that the instance is in a finalised or non-configurable state.
    /// </summary>
    /// <remarks>
    /// This method is used to prevent reconfiguration of algorithm parameters such as the key, number of rounds, or other settings once
    /// hashing has begun. It ensures settings are immutable after initialisation.
    /// </remarks>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
    /// started or been finalised.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
