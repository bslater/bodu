// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hc128StreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the HC-128 keystream primitive specified by Hongjun Wu. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// HC-128 binds a 128-bit key and a 128-bit IV at construction and produces 32-bit keystream words from two 512-word
/// secret tables, <c>P</c> and <c>Q</c>. Like Rabbit it has no seekable block counter: the tables are large evolving
/// state and each step both updates one table entry and emits one keystream word, so the keystream must be produced
/// strictly in sequence — the contract the engine-owns-advancement <see cref="IStreamCipher" /> interface expresses.
/// </para>
/// <para>
/// Initialization expands the key and IV into a 1280-word array using the SHA-256 message-schedule functions, seeds the
/// <c>P</c> and <c>Q</c> tables, and then runs the cipher for 1024 steps, feeding each output back into the tables
/// before any keystream is released.
/// </para>
/// </remarks>
/// <seealso href="https://www.ecrypt.eu.org/stream/hc256pf.html">HC-128 (eSTREAM software portfolio)</seealso>
/// <seealso cref="Hc128" />
internal sealed class Hc128StreamCipher
    : IStreamCipher
{
    /// <summary>The required key length, in bytes (128 bits).</summary>
    internal const int KeySizeBytes = 16;

    /// <summary>The required IV length, in bytes (128 bits).</summary>
    internal const int NonceSizeBytes = 16;

    /// <summary>The keystream block length, in bytes (512 bits, i.e. sixteen 32-bit words).</summary>
    internal const int BlockSizeBytes = 64;

    /// <summary>The number of 32-bit words in each of the two secret tables.</summary>
    private const int TableSize = 512;

    /// <summary>The bit mask used to wrap table indices into the range <c>0</c> to <c>TableSize - 1</c>.</summary>
    private const int TableMask = TableSize - 1;

    /// <summary>The secret table <c>P</c>, holding 512 evolving 32-bit state words.</summary>
    private readonly uint[] _p = new uint[TableSize];

    /// <summary>The secret table <c>Q</c>, holding 512 evolving 32-bit state words.</summary>
    private readonly uint[] _q = new uint[TableSize];

    /// <summary>The step counter, taken modulo 1024 to alternate between the <c>P</c> and <c>Q</c> halves.</summary>
    private uint _counter;

    /// <summary>Indicates whether this instance has been disposed and its secret tables cleared.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hc128StreamCipher" /> class from the supplied key and IV.
    /// </summary>
    /// <param name="key">The 16-byte (128-bit) key.</param>
    /// <param name="nonce">The 16-byte (128-bit) IV.</param>
    /// <remarks>
    /// The caller is responsible for validating the lengths.
    /// </remarks>
    internal Hc128StreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        Initialize(key, nonce);
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeBytes;

    /// <inheritdoc />
    public void NextKeystreamBlock(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, BlockSizeBytes);

        for (int offset = 0; offset < BlockSizeBytes; offset += 4)
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset, 4), GenerateWord());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_p);
        CryptographyHelper.Clear(_q);
        _counter = 0;
        _disposed = true;
    }

    /// <summary>
    /// The first SHA-256-style message-schedule function used during initialization.
    /// </summary>
    /// <param name="x">The input word.</param>
    /// <returns><c>(x &gt;&gt;&gt; 7) ^ (x &gt;&gt;&gt; 18) ^ (x &gt;&gt; 3)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint F1(uint x) =>
        BitOperations.RotateRight(x, 7) ^ BitOperations.RotateRight(x, 18) ^ (x >> 3);

    /// <summary>
    /// The second SHA-256-style message-schedule function used during initialization.
    /// </summary>
    /// <param name="x">The input word.</param>
    /// <returns><c>(x &gt;&gt;&gt; 17) ^ (x &gt;&gt;&gt; 19) ^ (x &gt;&gt; 10)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint F2(uint x) =>
        BitOperations.RotateRight(x, 17) ^ BitOperations.RotateRight(x, 19) ^ (x >> 10);

    /// <summary>
    /// The <c>g1</c> mixing function used when updating the <c>P</c> table.
    /// </summary>
    /// <param name="x">The first input word.</param>
    /// <param name="y">The second input word.</param>
    /// <param name="z">The third input word.</param>
    /// <returns><c>((x &gt;&gt;&gt; 10) ^ (z &gt;&gt;&gt; 23)) + (y &gt;&gt;&gt; 8)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint G1(uint x, uint y, uint z) =>
        unchecked((BitOperations.RotateRight(x, 10) ^ BitOperations.RotateRight(z, 23)) + BitOperations.RotateRight(y, 8));

    /// <summary>
    /// The <c>g2</c> mixing function used when updating the <c>Q</c> table.
    /// </summary>
    /// <param name="x">The first input word.</param>
    /// <param name="y">The second input word.</param>
    /// <param name="z">The third input word.</param>
    /// <returns><c>((x &lt;&lt;&lt; 10) ^ (z &lt;&lt;&lt; 23)) + (y &lt;&lt;&lt; 8)</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint G2(uint x, uint y, uint z) =>
        unchecked((BitOperations.RotateLeft(x, 10) ^ BitOperations.RotateLeft(z, 23)) + BitOperations.RotateLeft(y, 8));

    /// <summary>
    /// The <c>h1</c> table-lookup function, indexing the <c>Q</c> table by the low and high-middle bytes of
    /// <paramref name="x" />.
    /// </summary>
    /// <param name="x">The word selecting the table indices.</param>
    /// <returns><c>Q[x byte0] + Q[256 + x byte2]</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint H1(uint x) =>
        unchecked(_q[(byte)x] + _q[256 + (byte)(x >> 16)]);

    /// <summary>
    /// The <c>h2</c> table-lookup function, indexing the <c>P</c> table by the low and high-middle bytes of
    /// <paramref name="x" />.
    /// </summary>
    /// <param name="x">The word selecting the table indices.</param>
    /// <returns><c>P[x byte0] + P[256 + x byte2]</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint H2(uint x) =>
        unchecked(_p[(byte)x] + _p[256 + (byte)(x >> 16)]);

    /// <summary>
    /// Expands the key and IV, seeds the <c>P</c> and <c>Q</c> tables, and runs the 1024 self-feeding warm-up steps.
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    /// <param name="nonce">The 16-byte IV.</param>
    private void Initialize(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        // The expansion buffer is seeded directly from the key and IV and holds the entire key-derived schedule;
        // it is zeroed in the finally so a fault mid-initialization cannot leave it live on the stack.
        Span<uint> w = stackalloc uint[1280];

        try
        {
            // W[0..7] = key (repeated), W[8..15] = IV (repeated).
            for (int i = 0; i < 4; i++)
            {
                uint k = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));
                w[i] = k;
                w[i + 4] = k;
            }

            for (int i = 0; i < 4; i++)
            {
                uint v = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(i * 4, 4));
                w[i + 8] = v;
                w[i + 12] = v;
            }

            for (int i = 16; i < 1280; i++)
                w[i] = unchecked(F2(w[i - 2]) + w[i - 7] + F1(w[i - 15]) + w[i - 16] + (uint)i);

            for (int i = 0; i < TableSize; i++)
            {
                _p[i] = w[i + 256];
                _q[i] = w[i + 768];
            }
        }
        finally
        {
            CryptographyHelper.Clear(w);
        }

        // Run 1024 steps, feeding the output back into the tables instead of releasing it.
        for (int i = 0; i < 1024; i++)
        {
            int j = i & TableMask;
            if ((i & 1023) < 512)
                _p[j] = unchecked((_p[j] + G1(_p[(j - 3) & TableMask], _p[(j - 10) & TableMask], _p[(j - 511) & TableMask])) ^ H1(_p[(j - 12) & TableMask]));
            else
                _q[j] = unchecked((_q[j] + G2(_q[(j - 3) & TableMask], _q[(j - 10) & TableMask], _q[(j - 511) & TableMask])) ^ H2(_q[(j - 12) & TableMask]));
        }

        _counter = 0;
    }

    /// <summary>
    /// Updates one table entry and emits the next 32-bit keystream word.
    /// </summary>
    /// <returns>The next keystream word.</returns>
    private uint GenerateWord()
    {
        int j = (int)(_counter & TableMask);
        uint word;

        if ((_counter & 1023) < 512)
        {
            _p[j] = unchecked(_p[j] + G1(_p[(j - 3) & TableMask], _p[(j - 10) & TableMask], _p[(j - 511) & TableMask]));
            word = H1(_p[(j - 12) & TableMask]) ^ _p[j];
        }
        else
        {
            _q[j] = unchecked(_q[j] + G2(_q[(j - 3) & TableMask], _q[(j - 10) & TableMask], _q[(j - 511) & TableMask]));
            word = H2(_q[(j - 12) & TableMask]) ^ _q[j];
        }

        _counter = (_counter + 1) & 1023;
        return word;
    }
}
