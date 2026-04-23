
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for <c>Adler-32</c> style checksum algorithms using <see cref="uint" /> accumulators.
/// </summary>
/// <remarks>
/// <para>
/// This class specializes the generic <see cref="Adler{T}" /> base class for 32-bit checksums. It implements finalization logic
/// specific to Adler-32 variants, combining the A and B accumulators into a 32-bit hash as: <c><![CDATA[(B << 16) | A]]></c>.
/// </para>
/// <para>
/// Derived classes such as <see cref="Adler32" /> and <see cref="Adler32C" /> supply different moduli (e.g., 65521 or 65536) depending
/// on performance or compatibility needs.
/// </para>
/// </remarks>
public abstract class Adler32Base
    : Bodu.Security.Cryptography.Adler<uint>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Adler32Base" /> class.
    /// </summary>
    protected Adler32Base(uint modulo)
        : base(modulo)
    { }

    /// <summary>
    /// Finalises the Adler checksum and returns it as a 4-byte array.
    /// </summary>
    /// <returns>
    /// A 4-byte array containing the checksum encoded in <b>big-endian</b> byte order as <c><![CDATA[(B << 16) | A]]></c>.
    /// </returns>
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
