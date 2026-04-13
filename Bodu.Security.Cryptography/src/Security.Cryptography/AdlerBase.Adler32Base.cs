namespace Bodu.Security.Cryptography
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a reusable base class for <c>Adler-32</c> style checksum algorithms using <see cref="uint" /> accumulators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class specializes the generic <see cref="AdlerBase{T}" /> base class for 32-bit checksums. It implements finalization logic
    /// specific to Adler-32 variants, combining the A and B accumulators into a 32-bit hash as: <c><![CDATA[(B << 16) | A]]></c>.
    /// </para>
    /// <para>
    /// Derived classes such as <see cref="Adler32" /> and <see cref="Adler32C" /> supply different moduli (e.g., 65521 or 65536) depending
    /// on performance or compatibility needs.
    /// </para>
    /// </remarks>
    public abstract class Adler32Base
        : Bodu.Security.Cryptography.AdlerBase<uint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32Base" /> class.
        /// </summary>
        protected Adler32Base(uint modulo)
            : base(modulo)
        { }

        /// <summary>
        /// Finalizes the hash computation and returns the resulting 32-bit <see cref="Adler32Base" /> hash in big-endian format. This
        /// method reflects all input previously processed via <see cref="HashAlgorithm.HashCore(byte[], int, int)" /> or
        /// <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> and produces a final, stable hash output.
        /// </summary>
        /// <returns>
        /// A 4-byte array representing the computed <c>Adler</c> hash value. The result is encoded in <b>big-endian</b> byte order as
        /// <c><![CDATA[(B << 16) | A]]></c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method completes the internal state of the hashing algorithm and serializes the final hash value into a
        /// platform-independent format. It is invoked automatically by <see cref="HashAlgorithm.ComputeHash(byte[])" /> and related methods
        /// once all data has been processed.
        /// </para>
        /// <para>After this method returns, the internal state is considered finalized and the computed hash is stable.</para>
        /// <para>
        /// In .NET 6.0 and later, the algorithm is automatically reset by invoking <see cref="HashAlgorithm.Initialize" />, allowing the
        /// instance to be reused immediately.
        /// </para>
        /// <para>
        /// In earlier versions of .NET, the internal state is marked as finalized, and any subsequent calls to
        /// <see cref="HashAlgorithm.HashCore(byte[], int, int)" />, <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" />, or
        /// <see cref="HashAlgorithm.HashFinal" /> will throw a <see cref="CryptographicUnexpectedOperationException" />. To compute another
        /// hash, you must explicitly call <see cref="HashAlgorithm.Initialize" /> to reset the algorithm.
        /// </para>
        /// <para>
        /// Implementations should ensure all residual or pending data is processed and integrated into the final hash value before returning.
        /// </para>
        /// </remarks>
        protected override byte[] HashFinal()
        {
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

            finalized = true;
            State = 2;
#endif
            this.ThrowIfDisposed();

            uint hash = (this.PartB << 16) | this.PartA;
            Span<byte> span = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(span, hash); // Explicit big-endian output
            return span.ToArray();
        }
    }
}