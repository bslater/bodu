// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Blake3Core" />, the BLAKE3 compression function behind <see cref="Blake3" />, grouped into
/// member-named partial files. Each kernel is driven explicitly, whichever one dispatch picks, through a minimal
/// BLAKE3 built on the compression function alone, which covers the keyed hash and key derivation modes that
/// <see cref="Blake3" /> does not expose.
/// </summary>
[TestClass]
public sealed partial class Blake3CoreTests
{
    /// <summary>The resource name of the embedded official BLAKE3 test vectors.</summary>
    private const string Blake3KatResourceName = "Bodu.Security.Cryptography.Blake3.test_vectors.json";

    /// <summary>
    /// Returns every entry of the official test_vectors.json in one mode, truncated to the 256-bit hash.
    /// </summary>
    /// <param name="read">The reader method for the mode.</param>
    /// <returns>The vectors.</returns>
    private static List<MessageDigestKnownAnswer> ReadReferenceVectors(Func<Stream, int, string?, IEnumerable<MessageDigestKnownAnswer>> read)
    {
        using Stream stream = typeof(Blake3CoreTests).Assembly.GetManifestResourceStream(Blake3KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake3KatResourceName}' is missing.");

        return [.. read(stream, 32, "BLAKE3 KAT")];
    }

    /// <summary>
    /// Computes the first 32 bytes of a BLAKE3 output with the specified kernel, straight from the specification: every
    /// chunk compressed block by block from the key, and the chunks' chaining values combined in the specification's
    /// tree, the left subtree of every parent holding the largest power of two of chunks that leaves the right subtree
    /// at least one byte.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="key">The eight-word key: the IV for the unkeyed hash.</param>
    /// <param name="flags">The mode's flags, applied to every compression.</param>
    /// <param name="input">The input.</param>
    /// <returns>The first 32 bytes of the output.</returns>
    private static byte[] Hash(Blake3Core.KernelKind kernel, ReadOnlySpan<uint> key, uint flags, ReadOnlySpan<byte> input)
    {
        int chunks = Math.Max(1, (input.Length + Blake3Core.ChunkBytes - 1) / Blake3Core.ChunkBytes);
        uint[][] chainingValues = new uint[chunks][];
        for (int chunk = 0; chunk < chunks; chunk++)
        {
            int start = chunk * Blake3Core.ChunkBytes;
            int length = Math.Min(Blake3Core.ChunkBytes, input.Length - start);
            chainingValues[chunk] = CompressChunk(kernel, key, flags, input.Slice(start, length), (ulong)chunk, isRoot: chunks == 1);
        }

        uint[] root = Subtree(kernel, key, flags, chainingValues, 0, chunks, isRoot: true);

        byte[] output = new byte[32];
        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(i * sizeof(uint)), root[i]);

        return output;
    }

    /// <summary>
    /// Computes a BLAKE3 keyed hash with the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="input">The input.</param>
    /// <returns>The first 32 bytes of the output.</returns>
    private static byte[] KeyedHash(Blake3Core.KernelKind kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> input) =>
        Hash(kernel, ToWords(key), Blake3Core.KeyedHash, input);

    /// <summary>
    /// Derives a key with the specified kernel: the context string hashed into a context key, and the key material
    /// hashed under it.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="context">The ASCII context string.</param>
    /// <param name="material">The key material.</param>
    /// <returns>The first 32 bytes of the derived key.</returns>
    private static byte[] DeriveKey(Blake3Core.KernelKind kernel, ReadOnlySpan<byte> context, ReadOnlySpan<byte> material)
    {
        byte[] contextKey = Hash(kernel, Blake3Core.InitializationVector, Blake3Core.DeriveKeyContext, context);

        return Hash(kernel, ToWords(contextKey), Blake3Core.DeriveKeyMaterial, material);
    }

    /// <summary>
    /// Compresses one chunk, block by block, into its chaining value.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chunk">The chunk: up to 1024 bytes, and empty only for an empty input.</param>
    /// <param name="counter">The chunk's index.</param>
    /// <param name="isRoot"><see langword="true" /> when the chunk is the whole input.</param>
    /// <returns>The chunk's chaining value.</returns>
    private static uint[] CompressChunk(Blake3Core.KernelKind kernel, ReadOnlySpan<uint> key, uint flags, ReadOnlySpan<byte> chunk, ulong counter, bool isRoot)
    {
        uint[] chainingValue = key.ToArray();
        int blocks = Math.Max(1, (chunk.Length + Blake3Core.BlockBytes - 1) / Blake3Core.BlockBytes);
        byte[] block = new byte[Blake3Core.BlockBytes];

        for (int index = 0; index < blocks; index++)
        {
            int length = Math.Min(Blake3Core.BlockBytes, chunk.Length - (index * Blake3Core.BlockBytes));
            Array.Clear(block);
            chunk.Slice(index * Blake3Core.BlockBytes, length).CopyTo(block);

            uint blockFlags = flags;
            if (index == 0) blockFlags |= Blake3Core.ChunkStart;
            if (index == blocks - 1) blockFlags |= isRoot ? Blake3Core.ChunkEnd | Blake3Core.Root : Blake3Core.ChunkEnd;

            Blake3Core.Compress(kernel, chainingValue, block, counter, (uint)length, blockFlags);
        }

        return chainingValue;
    }

    /// <summary>
    /// Combines a run of chunk chaining values into the chaining value of their subtree.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every parent.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValues">Every chunk's chaining value.</param>
    /// <param name="first">The index of the subtree's first chunk.</param>
    /// <param name="count">The number of chunks in the subtree.</param>
    /// <param name="isRoot"><see langword="true" /> when the subtree is the whole tree.</param>
    /// <returns>The subtree's chaining value.</returns>
    private static uint[] Subtree(Blake3Core.KernelKind kernel, ReadOnlySpan<uint> key, uint flags, uint[][] chainingValues, int first, int count, bool isRoot)
    {
        if (count == 1)
            return chainingValues[first];

        int left = 1 << BitOperations.Log2((uint)(count - 1));
        uint[] leftValue = Subtree(kernel, key, flags, chainingValues, first, left, isRoot: false);
        uint[] rightValue = Subtree(kernel, key, flags, chainingValues, first + left, count - left, isRoot: false);

        byte[] block = new byte[Blake3Core.BlockBytes];
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(i * sizeof(uint)), leftValue[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(32 + (i * sizeof(uint))), rightValue[i]);
        }

        uint[] chainingValue = key.ToArray();
        uint parentFlags = flags | Blake3Core.Parent;
        if (isRoot) parentFlags |= Blake3Core.Root;

        Blake3Core.Compress(kernel, chainingValue, block, 0, Blake3Core.BlockBytes, parentFlags);
        return chainingValue;
    }

    /// <summary>
    /// Reads a 32-byte key as eight little-endian words.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The words.</returns>
    private static uint[] ToWords(ReadOnlySpan<byte> key)
    {
        uint[] words = new uint[8];
        for (int i = 0; i < words.Length; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(key[(i * sizeof(uint))..]);

        return words;
    }

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static Blake3Core.KernelKind ParseSupportedKernel(string name)
    {
        Blake3Core.KernelKind kernel = Enum.Parse<Blake3Core.KernelKind>(name);
        if (!Blake3Core.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
