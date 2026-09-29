// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Poly1305Core" />, the Poly1305 authenticator behind <see cref="Poly1305" /> and the Poly1305
/// AEADs, grouped into member-named partial files. Tags are held to the published RFC 8439 vectors and, over seeded
/// keys, messages and ways of splitting them, to <see cref="Poly1305Reference" />, the radix-2^26 arithmetic the core
/// replaced. Every block loop — the scalar loop and each vector kernel, driven explicitly whichever one dispatch picks —
/// is held to both.
/// </summary>
[TestClass]
public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Computes a tag with one call each to <see cref="Poly1305Core.Initialize" />,
    /// <see cref="Poly1305Core.Update(ReadOnlySpan{byte})" /> and <see cref="Poly1305Core.Finish" />.
    /// </summary>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 16-byte tag.</returns>
    private static byte[] ComputeTag(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message) =>
        ComputeTag(Poly1305Core.KernelKind.Auto, key, message);

    /// <summary>
    /// Computes a tag with one call each to <see cref="Poly1305Core.Initialize" />,
    /// <see cref="Poly1305Core.Update(Poly1305Core.KernelKind, ReadOnlySpan{byte})" /> through the specified kernel, and
    /// <see cref="Poly1305Core.Finish" />.
    /// </summary>
    /// <param name="kernel">The kernel for the run of whole blocks.</param>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 16-byte tag.</returns>
    private static byte[] ComputeTag(Poly1305Core.KernelKind kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        Poly1305Core core = default;
        core.Initialize(key);
        core.Update(kernel, message);

        byte[] tag = new byte[Poly1305Core.TagBytes];
        core.Finish(tag);
        return tag;
    }

    /// <summary>
    /// Computes a tag with the message fed to <see cref="Poly1305Core.Update(ReadOnlySpan{byte})" /> in seeded random
    /// pieces, empty pieces included.
    /// </summary>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="message">The message.</param>
    /// <param name="random">The source of the piece lengths.</param>
    /// <param name="maxPiece">The longest piece, in bytes.</param>
    /// <returns>The 16-byte tag.</returns>
    private static byte[] ComputeTagInPieces(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, Random random, int maxPiece) =>
        ComputeTagInPieces(Poly1305Core.KernelKind.Auto, key, message, random, maxPiece);

    /// <summary>
    /// Computes a tag with the message fed to <see cref="Poly1305Core.Update(Poly1305Core.KernelKind, ReadOnlySpan{byte})" />
    /// through the specified kernel, in seeded random pieces, empty pieces included.
    /// </summary>
    /// <param name="kernel">The kernel for each piece's run of whole blocks.</param>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="message">The message.</param>
    /// <param name="random">The source of the piece lengths.</param>
    /// <param name="maxPiece">The longest piece, in bytes.</param>
    /// <returns>The 16-byte tag.</returns>
    private static byte[] ComputeTagInPieces(Poly1305Core.KernelKind kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, Random random, int maxPiece)
    {
        Poly1305Core core = default;
        core.Initialize(key);

        int offset = 0;
        while (offset < message.Length)
        {
            int piece = Math.Min(random.Next(maxPiece + 1), message.Length - offset);
            core.Update(kernel, message.Slice(offset, piece));
            offset += piece;
        }

        byte[] tag = new byte[Poly1305Core.TagBytes];
        core.Finish(tag);
        return tag;
    }

    /// <summary>
    /// Parses a kernel name, marking the test inconclusive when the processor cannot run that kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static Poly1305Core.KernelKind ParseSupportedKernel(string name)
    {
        Poly1305Core.KernelKind kernel = Enum.Parse<Poly1305Core.KernelKind>(name);
        if (!Poly1305Core.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }

    /// <summary>
    /// Returns whether every byte of a core — key schedule, accumulator and held bytes alike — is zero.
    /// </summary>
    /// <param name="core">The core.</param>
    /// <returns><see langword="true" /> when the core holds only zeros.</returns>
    private static bool IsCleared(ref Poly1305Core core) =>
        !MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref core, 1)).ContainsAnyExcept((byte)0);

    /// <summary>
    /// Returns a new array of seeded random bytes.
    /// </summary>
    /// <param name="random">The source of the bytes.</param>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    private static byte[] NextBytes(Random random, int length)
    {
        byte[] bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }
}
