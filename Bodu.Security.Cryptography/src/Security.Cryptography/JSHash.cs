
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Justin Sobel's JSHash bitwise mixing function. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte, JSHash updates the running hash as <c><![CDATA[hash ^= (hash << 5) + (hash >> 2) + byte]]></c>. The final value
/// is returned in the platform's native byte order; consumers needing a specific endianness should normalise with
/// <see cref="System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(uint)" />.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class JSHash
    : System.Security.Cryptography.HashAlgorithm
{
    private const uint DefaultValue = 0x4E67C6A7;

    private bool disposed = false;
    private uint workingHash;
#if !NET6_0_OR_GREATER

    // Required for .NET Standard 2.0 or older frameworks
    private bool finalized;
#endif

    /// <summary>
    /// Initialises a new instance of the <see cref="JSHash" /> class.
    /// </summary>
    /// <remarks>This constructor initialises the hash algorithm to a default 32-bit output with a seed value of <c>0x4E67C6A7</c>.</remarks>
    public JSHash()
    {
        this.HashSizeValue = 32;
        this.Initialize();
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    public override void Initialize()
    {
#if !NET6_0_OR_GREATER
        State = 0;
        finalized = false;
#endif
        this.ThrowIfDisposed();
        this.workingHash = DefaultValue;
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
        if (this.disposed)
            return;

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
    /// Processes a segment of the input byte array and feeds it into the <see cref="JSHash" /> hashing algorithm. This method updates
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
    /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="JSHash" /> hashing algorithm.
    /// This method updates the internal hash state accordingly by consuming the entire input span.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();

        var v = this.workingHash;
        foreach (byte b in source)
        {
            v ^= (v << 5) + (v >> 2) + b;
        }

        this.workingHash = v;
    }

    /// <summary>
    /// Finalises the JSHash computation and returns the 32-bit result as a 4-byte array in native byte order.
    /// </summary>
    /// <returns>A 4-byte array containing the hash value in the platform's native byte order.</returns>
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
        MemoryMarshal.Write(span, in this.workingHash);
        return span.ToArray();
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the instance has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the algorithm has been disposed and further access is attempted.</exception>
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this.disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(JSHash));
#endif
    }
}
