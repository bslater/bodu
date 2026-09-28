// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="ScryptCore" />, the RFC 7914 engine behind <see cref="Scrypt" />, grouped into member-named
/// partial files. RFC 7914's intermediate vectors pin ROMix directly; the end-to-end vectors and the OpenSSL corpus
/// live in <see cref="ScryptTests" />.
/// </summary>
[TestClass]
public sealed partial class ScryptCoreTests
{
    /// <summary>The input block of RFC 7914, Section 10's scryptROMix vector, with <c>r = 1</c> and <c>N = 16</c>.</summary>
    private const string RomixInputHex =
        "f7ce0b653d2d72a4108cf5abe912ffdd777616dbbb27a70e8204f3ae2d0f6fad" +
        "89f68f4811d1e87bcc3bd7400a9ffd29094f0184639574f39ae5a1315217bcd7" +
        "894991447213bb226c25b54da86370fbcd984380374666bb8ffcb5bf40c254b0" +
        "67d27c51ce4ad5fed829c90b505a571b7f4d1cad6a523cda770e67bceaaf7e89";

    /// <summary>The output block of RFC 7914, Section 10's scryptROMix vector.</summary>
    private const string RomixOutputHex =
        "79ccc193629debca047f0b70604bf6b62ce3dd4a9626e355fafc6198e6ea2b46" +
        "d58413673b99b029d665c357601fb426a0b2f4bba200ee9f0a43d19b571a9c71" +
        "ef1142e65d5a266fddca832ce59faa7cac0b9cf1be2bffca300d01ee387619c4" +
        "ae12fd4438f203a0e4e1c47ec314861f4e9087cb33396a6873e8f9d2539a4b8e";

    /// <summary>The key of RFC 7914, Section 12's second vector: P = "password", S = "NaCl", N = 1024, r = 8, p = 16.</summary>
    private const string Rfc7914SecondKeyHex =
        "fdbabe1c9d3472007856e7190d01e9fe7c6ad7cbc8237830e77376634b373162" +
        "2eaf30d92e22a3886ff109279d9830dac727afb94a83ee6d8360cbdfa2cc0640";

    /// <summary>An idle timeout long enough that a pool's own timer never fires during a test.</summary>
    private static readonly TimeSpan LongIdleTimeout = TimeSpan.FromHours(1);

    /// <summary>
    /// Derives RFC 7914, Section 12's second vector with the workspace taken from the specified pool.
    /// </summary>
    /// <param name="pool">The pool the derivation takes its workspace from.</param>
    /// <returns>The derived key, as lowercase hex.</returns>
    private static string DeriveSecondRfc7914Key(NativeBufferPool pool)
    {
        byte[] key = new byte[64];
        ScryptCore.DeriveKey(Encoding.ASCII.GetBytes("password"), Encoding.ASCII.GetBytes("NaCl"), 1024, 8, 16, key, pool);
        return Convert.ToHexString(key).ToLowerInvariant();
    }

    /// <summary>
    /// Creates a pool that retains up to the specified number of buffers of up to 64 MiB, released after an hour idle.
    /// </summary>
    /// <param name="maxRetainedBuffers">The greatest number of buffers the pool retains.</param>
    /// <returns>The pool.</returns>
    private static NativeBufferPool CreatePool(int maxRetainedBuffers = 4) =>
        new(maxRetainedBuffers, 64L * 1024 * 1024, LongIdleTimeout, TimeProvider.System);

    /// <summary>
    /// Returns a deterministic pseudo-random sequence of words.
    /// </summary>
    /// <param name="length">The number of words.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The words.</returns>
    private static uint[] RandomWords(int length, int seed)
    {
        byte[] bytes = new byte[length * sizeof(uint)];
        new Random(seed).NextBytes(bytes);
        return ToWords(bytes);
    }

    /// <summary>
    /// Formats words as the hex of their little-endian bytes, the byte order RFC 7914 prints blocks in.
    /// </summary>
    /// <param name="words">The words.</param>
    /// <returns>The lowercase hex.</returns>
    private static string ToHex(ReadOnlySpan<uint> words)
    {
        byte[] bytes = new byte[words.Length * sizeof(uint)];
        for (int i = 0; i < words.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)), words[i]);

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Parses hex into the little-endian words RFC 7914 packs a block into.
    /// </summary>
    /// <param name="hex">The hex.</param>
    /// <returns>The words.</returns>
    private static uint[] ToWords(string hex) =>
        ToWords(Convert.FromHexString(hex));

    /// <summary>
    /// Reads bytes as little-endian words.
    /// </summary>
    /// <param name="bytes">The bytes; a multiple of four long.</param>
    /// <returns>The words.</returns>
    private static uint[] ToWords(byte[] bytes)
    {
        uint[] words = new uint[bytes.Length / sizeof(uint)];
        for (int i = 0; i < words.Length; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)));

        return words;
    }
}
