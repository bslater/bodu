// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Variant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the Base58 alphabet used by <see cref="Base58" />.
/// </summary>
/// <remarks>
/// Base58 is a non-power-of-two radix that excludes visually ambiguous characters (zero, capital-O, capital-I, and
/// lower-case L). Each variant differs only in the ordering of the 58 alphabet symbols.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] payload = { 0x00, 0x01, 0x02 };
///
/// var bitcoin = Base58.Encode(payload);                          // Bitcoin alphabet (default)
/// var flickr = Base58.Encode(payload, Base58Variant.Flickr);     // lowercase-first alphabet
/// var ripple = Base58.Encode(payload, Base58Variant.Ripple);     // Ripple's reordering
///]]>
/// </code>
/// </example>
/// </remarks>
public enum Base58Variant : byte
{
    /// <summary>
    /// The Bitcoin / Flickr alphabet, where digits are sorted ascending followed by upper-case letters and then
    /// lower-case letters (omitting <c>0</c>, <c>O</c>, <c>I</c>, <c>l</c>). This is the alphabet used by Bitcoin
    /// addresses, IPFS CIDs, Solana, NEAR, Stellar, and Flickr short URLs.
    /// </summary>
    BitcoinFlickr = 0,

    /// <summary>
    /// The Ripple alphabet, a permutation of the Bitcoin alphabet used by the XRP ledger.
    /// </summary>
    Ripple = 1,
}
