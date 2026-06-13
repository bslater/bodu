// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides unit tests for the <see cref="HashingStream" /> type, with member-specific coverage split across the
/// partial files of this class.
/// </summary>
[TestClass]
public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Computes the reference digest of the supplied bytes using a fresh <see cref="Fnv1a32" /> instance, for
    /// comparison against digests accumulated through a <see cref="HashingStream" />.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The expected digest bytes.</returns>
    private static byte[] ComputeExpectedHash(byte[] data)
    {
        Fnv1a32 reference = new();
        reference.Append(data);
        return reference.GetCurrentHash();
    }

    /// <summary>
    /// Creates a deterministic test payload of the specified length.
    /// </summary>
    /// <param name="length">The payload length in bytes.</param>
    /// <returns>An array whose bytes cycle through <c>0x00</c>–<c>0xFF</c>.</returns>
    private static byte[] CreatePayload(int length)
    {
        var payload = new byte[length];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte)i;

        return payload;
    }
}
