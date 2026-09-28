// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Blake2sCore" />, the BLAKE2s compression function behind <see cref="Blake2s" />, grouped into
/// member-named partial files. Each kernel is driven explicitly, whichever one dispatch picks, through a minimal
/// BLAKE2s built on the compression function alone.
/// </summary>
[TestClass]
public sealed partial class Blake2sCoreTests
{
    /// <summary>The resource name of the embedded official BLAKE2 test vectors.</summary>
    private const string Blake2KatResourceName = "Bodu.Security.Cryptography.Blake2.blake2-kat.json";

    /// <summary>
    /// Returns every BLAKE2s entry of the official blake2-kat.json: an incrementing message of 0 to 255 bytes, unkeyed
    /// and under a full-length key.
    /// </summary>
    /// <returns>The vectors.</returns>
    private static IEnumerable<MessageDigestKnownAnswer> ReadReferenceVectors()
    {
        using Stream stream = typeof(Blake2sCoreTests).Assembly.GetManifestResourceStream(Blake2KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake2KatResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in Blake2KatReader.Read(stream, "blake2s", "BLAKE2 KAT"))
            yield return vector;
    }

    /// <summary>
    /// Computes an BLAKE2s digest with the specified kernel, following RFC 7693, Section 3.3: the parameter block
    /// folded into the IV, the key zero-padded into a first block of its own, and the last block compressed with the
    /// finalization flag.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="key">The key; empty for an unkeyed digest.</param>
    /// <param name="message">The message.</param>
    /// <param name="digestLength">The digest length, in bytes.</param>
    /// <returns>The digest.</returns>
    private static byte[] Hash(Blake2sCore.KernelKind kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, int digestLength)
    {
        byte[] data = new byte[(key.IsEmpty ? 0 : 64) + message.Length];
        key.CopyTo(data);
        message.CopyTo(data.AsSpan(key.IsEmpty ? 0 : 64));

        uint[] state = new uint[Blake2sCore.StateWords];
        Blake2sCore.InitializationVector.CopyTo(state);
        state[0] ^= 0x0101_0000U ^ (uint)(key.Length << 8) ^ (uint)digestLength;

        int offset = 0;
        ulong counter = 0;
        while (data.Length - offset > 64)
        {
            counter += 64;
            Blake2sCore.Compress(kernel, state, data.AsSpan(offset, 64), counter, last: false);
            offset += 64;
        }

        byte[] last = new byte[64];
        data.AsSpan(offset).CopyTo(last);
        counter += (ulong)(data.Length - offset);
        Blake2sCore.Compress(kernel, state, last, counter, last: true);

        byte[] output = new byte[Blake2sCore.StateWords * sizeof(uint)];
        for (int i = 0; i < Blake2sCore.StateWords; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(i * sizeof(uint)), state[i]);

        return output[..digestLength];
    }

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static Blake2sCore.KernelKind ParseSupportedKernel(string name)
    {
        Blake2sCore.KernelKind kernel = Enum.Parse<Blake2sCore.KernelKind>(name);
        if (!Blake2sCore.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
