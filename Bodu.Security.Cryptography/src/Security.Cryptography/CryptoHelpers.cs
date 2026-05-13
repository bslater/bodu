// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides general-purpose utility methods used by cryptographic components and implementations within
/// <c>Bodu.Security.Cryptography</c>, including secure memory clearing, block padding and depadding, cryptographically
/// secure random byte generation, and argument validation helpers.
/// </summary>
/// <remarks>
/// <para>
/// This class is implemented as a partial type, with each file documenting a distinct facet of the surface: memory
/// clearing (<c>CryptoHelpers.DisposeHelpers.cs</c>), block padding (<c>CryptoHelpers.Padding.cs</c>), secure random byte
/// generation (<c>CryptoHelpers.RandomNumberGenerator.cs</c>), and internal argument validation helpers
/// (<c>CryptoHelpers.ThrowHelper.cs</c>).
/// </para>
/// <para>
/// Random byte generation methods use <see cref="RandomNumberGenerator"/> and operate on <see cref="Span{T}"/> where
/// possible to minimize allocations in performance-sensitive paths.
/// </para>
/// </remarks>
internal static partial class CryptoHelpers
{
    /// <summary>
    /// Returns a comma-separated string of valid key sizes (in bits) from the provided <see cref="KeySizes"/> array.
    /// </summary>
    /// <param name="keySizes">An array of <see cref="KeySizes"/> objects specifying allowed key sizes.</param>
    /// <returns>A string containing all supported key sizes in ascending order, or an empty string if none.</returns>
    internal static string FormatLegalSizes(KeySizes[]? keySizes) =>
        keySizes is null || keySizes.Length == 0
            ? string.Empty
            : string.Join(
                ", ",
                keySizes
                    .SelectMany(ks => ks.SkipSize == 0
                        ? [ks.MinSize]
                        : Enumerable.Range(0, ((ks.MaxSize - ks.MinSize) / ks.SkipSize) + 1)
                            .Select(i => ks.MinSize + (i * ks.SkipSize)))
                    .Distinct()
                    .Order());
}
