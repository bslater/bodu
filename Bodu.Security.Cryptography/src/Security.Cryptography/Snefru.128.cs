// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Snefru.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 128-bit (16-byte) hash using the <c>Snefru</c> hash algorithm by Ralph Merkle. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Snefru128" /> maintains a 4-word internal state and absorbs input in 48-byte blocks into a 512-bit working buffer,
/// applying 8 rounds of S-box substitution and word rotation per block. On finalisation the state is XOR-folded from the permuted
/// buffer and serialised in big-endian byte order. See <see cref="Snefru{T}" /> for shared background.
/// </para>
/// <note type="important">Snefru is considered broken and <b>not</b> suitable for password hashing, digital signatures, or secure
/// data integrity checks.</note>
/// </remarks>
public sealed class Snefru128 : Snefru<Snefru128>
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Snefru128" /> class using a fixed 128-bit output size.
    /// </summary>
    public Snefru128()
        : base(128)
    {
    }
}
