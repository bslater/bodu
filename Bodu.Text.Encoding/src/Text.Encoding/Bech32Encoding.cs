// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the checksum constant used by <see cref="Bech32" /> — the original Bech32 scheme defined by BIP-173 or
/// the Bech32m revision defined by BIP-350.
/// </summary>
/// <remarks>
/// The two schemes share an identical alphabet and checksum algorithm and differ only in the constant that the checksum
/// polynomial is XORed against (<c>1</c> for Bech32, <c>0x2BC830A3</c> for Bech32m). Bech32m was introduced to fix an
/// insertion/deletion weakness and is the scheme used by SegWit version 1 and later (Taproot) addresses.
/// </remarks>
public enum Bech32Encoding : byte
{
    /// <summary>
    /// The original Bech32 scheme defined by BIP-173 (checksum constant <c>1</c>).
    /// </summary>
    Bech32 = 0,

    /// <summary>
    /// The Bech32m scheme defined by BIP-350 (checksum constant <c>0x2BC830A3</c>).
    /// </summary>
    Bech32m = 1,
}
