// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32Base.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable base class for <c>Adler-32</c> style checksum algorithms using <see cref="uint" /> accumulators.
/// </summary>
/// <remarks>
/// <para>
/// Finalization combines the A and B accumulators into a 32-bit hash as <c><![CDATA[(B << 16) | A]]></c>, written in
/// big-endian byte order.
/// </para>
/// <para>
/// Derived classes such as <see cref="Adler32" /> and <see cref="Adler32C" /> supply different moduli (65521 or 65536)
/// depending on performance or compatibility needs.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Consume through a concrete derivative — the shared 32-bit finalization layout
/// // is identical across every Adler32Base subclass.
/// Adler32Base hash = new Adler32();                 // modulus 65521 (the canonical Adler-32)
/// hash.Append("Wikipedia"u8);
/// byte[] digest = hash.GetCurrentHash();            // 0x11E60398 in big-endian byte order
///]]>
/// </example>
public abstract class Adler32Base
    : Adler<uint>
{
    /// <summary>The digest length, in bytes, produced by the 32-bit Adler finalization.</summary>
    private const int HashLength = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler32Base" /> class with the specified modulus.
    /// </summary>
    /// <param name="modulo">The modulus applied to the accumulators after each reduction step.</param>
    protected Adler32Base(uint modulo)
        : base(HashLength, modulo)
    {
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        uint hash = (PartB << 16) | PartA;
        BinaryPrimitives.WriteUInt32BigEndian(destination, hash);
    }
}
