// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler64Base.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// Provides a reusable base class for <c>Adler-64</c> style checksum algorithms using <see cref="ulong" /> accumulators.
/// </summary>
/// <remarks>
/// <para>
/// This class specializes the generic <see cref="Adler{T}" /> base class for 64-bit checksums. It implements finalization logic
/// specific to Adler-64 variants, combining the A and B accumulators into a 64-bit hash as: <c><![CDATA[(B << 32) | A]]></c>.
/// </para>
/// <para>
/// Derived classes such as <see cref="Adler64" /> supply the modulus 4294967291 (2^32 − 5) to support wide-range checksums with reduced
/// wraparound risk compared to <see cref="Adler32Base" />.
/// </para>
/// </remarks>
public abstract class Adler64Base
    : Bodu.Security.Cryptography.Adler<ulong>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Adler64Base" /> class.
    /// </summary>
    protected Adler64Base(ulong modulo)
        : base(modulo)
    { }

    /// <summary>
    /// Finalises the Adler checksum and returns it as an 8-byte array.
    /// </summary>
    /// <returns>
    /// An 8-byte array containing the checksum encoded in <b>big-endian</b> byte order as <c><![CDATA[(B << 32) | A]]></c>.
    /// </returns>
    /// <exception cref="CryptographicUnexpectedOperationException">Thrown when the hash algorithm has been disposed or has produced an unexpected finalisation state.</exception>
    protected override byte[] HashFinal()
    {
#if !NET6_0_OR_GREATER
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

        finalized = true;
        State = 2;
#endif
        this.ThrowIfDisposed();

        ulong hash = (this.PartB << 32) | this.PartA;
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(span, hash); // Explicit big-endian output
        return span.ToArray();
    }
}
