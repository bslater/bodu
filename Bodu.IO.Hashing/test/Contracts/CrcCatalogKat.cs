// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcCatalogKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Represents a RevEng-style CRC catalogue row carrying every parameter required to fully describe a CRC variant:
/// width, polynomial, initial value, reflect-in/reflect-out flags, final XOR mask, and the reference
/// <see cref="Check" /> value computed for the canonical input <c>"123456789"</c>.
/// </summary>
/// <param name="Name">The CRC variant name, for example <c>"CRC-32/ISCSI"</c>.</param>
/// <param name="Width">The CRC width in bits.</param>
/// <param name="Polynomial">The generator polynomial in the bit order the variant uses.</param>
/// <param name="Init">The initial register value.</param>
/// <param name="RefIn">Whether input bytes are reflected.</param>
/// <param name="RefOut">Whether the output register is reflected.</param>
/// <param name="XorOut">The final XOR mask applied to the output register.</param>
/// <param name="Check">
/// The reference CRC value over the ASCII string <c>"123456789"</c>, used as the canonical conformance vector for every
/// catalogue entry.
/// </param>
public sealed record CrcCatalogKat(
    string Name,
    int Width,
    ulong Polynomial,
    ulong Init,
    bool RefIn,
    bool RefOut,
    ulong XorOut,
    ulong Check) : IKat;
