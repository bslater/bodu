// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SamplePayload.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines;

/// <summary>
/// Supplies the fixed payload and key material every scenario shares, so each run prints identical output.
/// </summary>
/// <remarks>
/// Fixed keys, IVs, and nonces are for reproducibility only. Real code draws a fresh IV or nonce for every message,
/// because reusing one under the same key leaks the relationship between the plaintexts, and loads keys from a key
/// store rather than a constant.
/// </remarks>
public static class SamplePayload
{
    /// <summary>The payload length in bytes: 200 KiB, larger than the 80 KiB default buffer, so every stream call reads more than once.</summary>
    public const int Length = 200 * 1024;

    /// <summary>Gets the fixed 256-bit key the ciphers use.</summary>
    public static byte[] Key { get; } = Fill(32, 0x10);

    /// <summary>Gets the fixed 128-bit IV the CBC scenarios use.</summary>
    public static byte[] Iv { get; } = Fill(16, 0x20);

    /// <summary>Gets the fixed 192-bit nonce XChaCha20 uses.</summary>
    public static byte[] Nonce { get; } = Fill(24, 0x30);

    /// <summary>
    /// Creates the deterministic payload: a byte pattern that does not repeat within a cipher block.
    /// </summary>
    /// <returns>A new <see cref="Length" />-byte array with the same contents on every call.</returns>
    public static byte[] Create()
    {
        var payload = new byte[Length];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte)((i * 31) ^ (i >> 8));

        return payload;
    }

    /// <summary>
    /// Creates a byte array of the given length whose bytes count up from a starting value.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <param name="start">The value of the first byte.</param>
    /// <returns>The filled array.</returns>
    public static byte[] Fill(int length, byte start)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
            bytes[i] = (byte)(start + i);

        return bytes;
    }
}
