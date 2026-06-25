// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler64Base.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a reusable base class for <c>Adler-64</c> style checksum algorithms using <see cref="ulong" />
/// accumulators.
/// </summary>
/// <remarks>
/// <para>
/// Finalization combines the A and B accumulators into a 64-bit hash as <c><![CDATA[(B << 32) | A]]></c>, written in
/// big-endian byte order.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Consume through a concrete derivative — the shared 64-bit finalization layout
/// // is identical across every Adler64Base subclass.
/// Adler64Base hash = new Adler64();
/// hash.Append("Wikipedia"u8);
/// byte[] digest = hash.GetCurrentHash();   // big-endian (B << 32) | A
///]]>
/// </example>
public abstract class Adler64Base
    : Adler<ulong>
{
    /// <summary>The digest length, in bytes, produced by the 64-bit Adler finalization.</summary>
    private const int HashLength = 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler64Base" /> class with the specified modulus.
    /// </summary>
    /// <param name="modulo">The modulus applied to the accumulators after each reduction step.</param>
    protected Adler64Base(ulong modulo)
        : base(HashLength, modulo)
    {
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        ulong hash = (partB << 32) | partA;
        BinaryPrimitives.WriteUInt64BigEndian(destination, hash);
    }
}
