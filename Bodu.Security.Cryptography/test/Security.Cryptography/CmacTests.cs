// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CmacTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Cmac" />, the incremental CMAC the EAX and SIV transforms share, grouped into member-named
/// partial files and a known-answer file.
/// </summary>
[TestClass]
public sealed partial class CmacTests
{
    /// <summary>The RFC 4493 Section 4 example key.</summary>
    private static readonly byte[] s_rfc4493Key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

    /// <summary>The RFC 4493 Section 4 example message, of which the examples MAC the first 0, 16, 40, and 64 bytes.</summary>
    private static readonly byte[] s_rfc4493Message = Convert.FromHexString(
        "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710");

    /// <summary>
    /// Computes a CMAC with <see cref="Cmac" />, appending the message in the given pieces.
    /// </summary>
    /// <param name="cipher">The block cipher keyed with the CMAC key.</param>
    /// <param name="message">The message.</param>
    /// <param name="pieces">The lengths of the pieces to append, which must sum to the message length.</param>
    /// <returns>The 16-byte MAC.</returns>
    private static byte[] ComputeInPieces(IBlockCipher cipher, byte[] message, params int[] pieces)
    {
        Span<byte> buffers = stackalloc byte[4 * Cmac.BlockBytes];
        Span<byte> k1 = buffers[..16];
        Span<byte> k2 = buffers.Slice(16, 16);
        Cmac.DeriveSubkeys(cipher, k1, k2);

        var cmac = new Cmac(cipher, k1, k2, buffers.Slice(32, 16), buffers.Slice(48, 16));
        int offset = 0;
        foreach (int piece in pieces)
        {
            cmac.Append(message.AsSpan(offset, piece));
            offset += piece;
        }

        byte[] mac = new byte[16];
        cmac.Finish(mac);
        return mac;
    }
}
