// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoKatInputs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides the canonical byte payloads reused as known-answer test inputs across cryptographic families — the empty
/// input, the strings <c>"ABC"</c> and the quick-brown-fox pangram, sixteen zero bytes, and the <c>0x00..0xFE</c>
/// sequence.
/// </summary>
/// <remarks>
/// Arrays are allocated once and shared across all test runs. Test code must treat them as immutable even though
/// <see cref="byte" /> arrays are mutable by the CLR — mutating a shared input would corrupt every other vector relying
/// on the same payload.
/// </remarks>
public static class CryptoKatInputs
{
    /// <summary>The empty input (zero bytes).</summary>
    public static readonly byte[] Empty = [];

    /// <summary>The three ASCII bytes of the string <c>"ABC"</c>.</summary>
    public static readonly byte[] Abc = Encoding.ASCII.GetBytes("ABC");

    /// <summary>The 43 ASCII bytes of the pangram <c>"The quick brown fox jumps over the lazy dog"</c>.</summary>
    public static readonly byte[] QuickBrownFox =
        Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <summary>Sixteen zero bytes.</summary>
    public static readonly byte[] Zeros16 = new byte[16];

    /// <summary>The 255-byte sequence <c>0x00, 0x01, …, 0xFE</c>.</summary>
    public static readonly byte[] Sequential0To255 =
        Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();
}
