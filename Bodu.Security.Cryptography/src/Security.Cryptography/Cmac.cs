// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Cmac.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes CMAC (NIST SP 800-38B, RFC 4493) over a message supplied in pieces, without copying it, for a 128-bit block
/// cipher.
/// </summary>
/// <remarks>
/// <para>
/// CMAC chains every block of the message through CBC-MAC and treats the last block specially: a complete last block is
/// XORed with the subkey <c>K1</c>, and a partial one - or the empty message - is padded with <c>10*</c> and XORed with
/// <c>K2</c>. <see cref="Append" /> therefore holds back the last block it has seen until more data or
/// <see cref="Finish" /> shows whether it is the last, and folds everything before it through <see cref="CbcChain" />
/// in whole runs, so a long message costs one chained call rather than one call per block.
/// </para>
/// <para>
/// The caller owns every buffer - the subkeys, the chaining state, and the held-back block - and clears them when the
/// computation ends.
/// </para>
/// </remarks>
internal ref struct Cmac
{
    /// <summary>The CMAC and CBC-MAC block size, in bytes.</summary>
    internal const int BlockBytes = 16;

    /// <summary>The block cipher keyed with the CMAC key.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The subkey <c>K1</c>, for a complete last block.</summary>
    private readonly ReadOnlySpan<byte> _k1;

    /// <summary>The subkey <c>K2</c>, for a padded last block.</summary>
    private readonly ReadOnlySpan<byte> _k2;

    /// <summary>The CBC-MAC chaining state.</summary>
    private readonly Span<byte> _state;

    /// <summary>The block held back until it is known whether it is the last.</summary>
    private readonly Span<byte> _pending;

    /// <summary>The number of bytes in <see cref="_pending" />.</summary>
    private int _pendingLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cmac" /> struct for a new message.
    /// </summary>
    /// <param name="cipher">The block cipher keyed with the CMAC key.</param>
    /// <param name="k1">The subkey <c>K1</c>, from <see cref="DeriveSubkeys" />.</param>
    /// <param name="k2">The subkey <c>K2</c>, from <see cref="DeriveSubkeys" />.</param>
    /// <param name="state">A 16-byte buffer for the chaining state; cleared here.</param>
    /// <param name="pending">A 16-byte buffer for the held-back block.</param>
    internal Cmac(IBlockCipher cipher, ReadOnlySpan<byte> k1, ReadOnlySpan<byte> k2, Span<byte> state, Span<byte> pending)
    {
        _cipher = cipher;
        _k1 = k1;
        _k2 = k2;
        _state = state;
        _pending = pending;
        _pendingLength = 0;
        state.Clear();
    }

    /// <summary>
    /// Derives the CMAC subkeys: <c>L = E_K(0¹²⁸)</c>, <c>K1 = dbl(L)</c>, and <c>K2 = dbl(K1)</c>.
    /// </summary>
    /// <param name="cipher">The block cipher keyed with the CMAC key.</param>
    /// <param name="k1">Receives the 16-byte subkey <c>K1</c>.</param>
    /// <param name="k2">Receives the 16-byte subkey <c>K2</c>.</param>
    internal static void DeriveSubkeys(IBlockCipher cipher, Span<byte> k1, Span<byte> k2)
    {
        Span<byte> zero = stackalloc byte[BlockBytes];
        zero.Clear();

        cipher.Encrypt(zero, k1);
        GaloisField128.Double(k1, k1);
        GaloisField128.Double(k1, k2);
    }

    /// <summary>
    /// Appends the next piece of the message.
    /// </summary>
    /// <param name="data">The piece to append; may be empty.</param>
    internal void Append(scoped ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return;

        // Top up the held-back block. It stays held back when the data ends there: it may be the last block.
        if (_pendingLength < BlockBytes)
        {
            int take = Math.Min(BlockBytes - _pendingLength, data.Length);
            data[..take].CopyTo(_pending[_pendingLength..]);
            _pendingLength += take;
            data = data[take..];

            if (data.IsEmpty)
                return;
        }

        // More data follows, so the held-back block is not the last. Chain it, then every whole block of the data but
        // the last, which is held back in its place.
        int keep = ((data.Length - 1) % BlockBytes) + 1;
        CbcChain.Mac(_cipher, _pending, _state);
        CbcChain.Mac(_cipher, data[..^keep], _state);

        data[^keep..].CopyTo(_pending);
        _pendingLength = keep;
    }

    /// <summary>
    /// Completes the MAC with the held-back last block.
    /// </summary>
    /// <param name="mac">Receives the 16-byte MAC; may be the state buffer itself.</param>
    internal void Finish(scoped Span<byte> mac)
    {
        if (_pendingLength == BlockBytes)
        {
            CryptographyHelper.Xor(_pending, _k1, _pending);
        }
        else
        {
            _pending[_pendingLength] = 0x80;
            _pending.Slice(_pendingLength + 1).Clear();
            CryptographyHelper.Xor(_pending, _k2, _pending);
        }

        CbcChain.Mac(_cipher, _pending, _state);
        _state.CopyTo(mac);
    }
}
