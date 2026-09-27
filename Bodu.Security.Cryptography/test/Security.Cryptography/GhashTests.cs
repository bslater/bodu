// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Ghash" />, grouped into member-named partial files. Every kernel the processor supports is held
/// to the published RFC 8452 and NIST SP 800-38D values and to a bit-serial reference built from
/// <see cref="GaloisField128.MultiplyScalar" />; GCM's and GCM-SIV's known-answer suites pin the kernels end to end.
/// </summary>
[TestClass]
public sealed partial class GhashTests
{
    /// <summary>
    /// Gets every kernel this processor can run, so an x64 host checks the carry-less and scalar kernels and an ARM64
    /// host with the cryptography extension the polynomial-multiply and scalar kernels.
    /// </summary>
    /// <returns>One row per supported kernel, holding its name.</returns>
    public static IEnumerable<object[]> SupportedKernels() =>
        Enum.GetValues<Ghash.KernelKind>().Where(Ghash.IsSupported).Select(kernel => new object[] { kernel.ToString() });

    /// <summary>
    /// Computes GHASH one block at a time with the bit-serial reference multiply.
    /// </summary>
    /// <param name="h">The hash key.</param>
    /// <param name="state">The starting state, in GHASH's byte order.</param>
    /// <param name="data">The data; a final partial block is padded with zeros.</param>
    /// <returns>The resulting state.</returns>
    private static byte[] ReferenceGhash(byte[] h, byte[] state, byte[] data)
    {
        byte[] y = (byte[])state.Clone();
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            for (int i = 0; i < Math.Min(16, data.Length - offset); i++)
                y[i] ^= data[offset + i];

            GaloisField128.MultiplyScalar(y, h, y);
        }

        return y;
    }

    /// <summary>
    /// Computes POLYVAL one block at a time through RFC 8452 Appendix A's isomorphism with GHASH, on the bit-serial
    /// reference multiply.
    /// </summary>
    /// <param name="h">The POLYVAL key.</param>
    /// <param name="state">The starting state, in POLYVAL's byte order.</param>
    /// <param name="data">The data; a final partial block is padded with zeros.</param>
    /// <returns>The resulting state.</returns>
    private static byte[] ReferencePolyval(byte[] h, byte[] state, byte[] data)
    {
        byte[] ghashKey = ReverseCopy(h);
        Ghash.MultiplyByX(ghashKey, ghashKey);

        byte[] y = ReverseCopy(state);
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            byte[] block = new byte[16];
            Array.Copy(data, offset, block, 0, Math.Min(16, data.Length - offset));
            for (int i = 0; i < 16; i++)
                y[i] ^= block[15 - i];

            GaloisField128.MultiplyScalar(y, ghashKey, y);
        }

        return ReverseCopy(y);
    }

    /// <summary>
    /// Returns a copy of a buffer with its bytes in reverse order.
    /// </summary>
    /// <param name="value">The buffer to reverse.</param>
    /// <returns>The reversed copy.</returns>
    private static byte[] ReverseCopy(byte[] value)
    {
        byte[] copy = (byte[])value.Clone();
        Array.Reverse(copy);
        return copy;
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] RandomBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
