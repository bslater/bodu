// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerHashingVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the padding variant used by the <see cref="Bodu.Security.Cryptography.Tiger"/> hashing algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TigerHashingVariant"/> enumeration differentiates between the original Tiger algorithm and the modified Tiger2
/// variant. These variants are identical in their core compression function but differ in how the final padding byte is applied during
/// hash finalization.
/// </para>
/// <para>
/// Pick <see cref="Tiger"/> when reproducing or interoperating with output from the original Anderson/Biham
/// reference implementation, and <see cref="Tiger2"/> when matching modern Tiger-based tooling or
/// TigerTree variants — most contemporary code paths use <see cref="Tiger2"/> by default. Aside from the
/// final padding byte the two variants are bit-identical, so the same key material and message produce
/// otherwise indistinguishable internal computation.
/// </para>
/// </remarks>
/// <seealso cref="Bodu.Security.Cryptography.Tiger"/>
public enum TigerHashingVariant
{
    /// <summary>
    /// Represents the original Tiger variant as defined by Ross Anderson and Eli Biham.
    /// </summary>
    /// <remarks>
    /// In this mode, the padding byte <c>0x01</c> is appended after the message data during the final block padding step. This is the
    /// canonical variant used in the original Tiger specification and reference implementation.
    /// </remarks>
    Tiger,

    /// <summary>
    /// Represents the Tiger2 variant of the Tiger hashing algorithm.
    /// </summary>
    /// <remarks>
    /// Tiger2 uses the padding byte <c>0x80</c> instead of <c>0x01</c> when finalizing the input. This change was introduced to improve
    /// compatibility with padding conventions in other cryptographic hash functions like MD5 and SHA. Aside from the padding
    /// difference, Tiger2 is identical in structure and performance to the original Tiger variant.
    /// </remarks>
    Tiger2
}
