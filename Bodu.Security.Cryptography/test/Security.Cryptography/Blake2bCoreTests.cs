// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Blake2bCore" />, the BLAKE2b compression function behind <see cref="Blake2b" />, grouped into
/// member-named partial files. Each kernel is driven explicitly, whichever one dispatch picks, through a minimal
/// BLAKE2b built on the compression function alone.
/// </summary>
[TestClass]
public sealed partial class Blake2bCoreTests
{
    /// <summary>The resource name of the embedded official BLAKE2 test vectors.</summary>
    private const string Blake2KatResourceName = "Bodu.Security.Cryptography.Blake2.blake2-kat.json";

    /// <summary>
    /// Returns every BLAKE2b entry of the official blake2-kat.json: an incrementing message of 0 to 255 bytes, unkeyed
    /// and under a full-length key.
    /// </summary>
    /// <returns>The vectors.</returns>
    private static IEnumerable<MessageDigestKnownAnswer> ReadReferenceVectors()
    {
        using Stream stream = typeof(Blake2bCoreTests).Assembly.GetManifestResourceStream(Blake2KatResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Blake2KatResourceName}' is missing.");

        foreach (MessageDigestKnownAnswer vector in Blake2KatReader.Read(stream, "blake2b", "BLAKE2 KAT"))
            yield return vector;
    }

    /// <summary>
    /// Computes an BLAKE2b digest with the specified kernel, following RFC 7693, Section 3.3: the parameter block
    /// folded into the IV, the key zero-padded into a first block of its own, and the last block compressed with the
    /// finalization flag.
    /// </summary>
    /// <param name="kernel">The kernel that compresses every block.</param>
    /// <param name="key">The key; empty for an unkeyed digest.</param>
    /// <param name="message">The message.</param>
    /// <param name="digestLength">The digest length, in bytes.</param>
    /// <returns>The digest.</returns>
    private static byte[] Hash(Blake2bCore.KernelKind kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, int digestLength)
    {
        byte[] data = new byte[(key.IsEmpty ? 0 : 128) + message.Length];
        key.CopyTo(data);
        message.CopyTo(data.AsSpan(key.IsEmpty ? 0 : 128));

        ulong[] state = new ulong[Blake2bCore.StateWords];
        Blake2bCore.InitializationVector.CopyTo(state);
        state[0] ^= 0x0101_0000U ^ (uint)(key.Length << 8) ^ (uint)digestLength;

        int offset = 0;
        ulong counter = 0;
        while (data.Length - offset > 128)
        {
            counter += 128;
            Blake2bCore.Compress(kernel, state, data.AsSpan(offset, 128), counter, last: false);
            offset += 128;
        }

        byte[] last = new byte[128];
        data.AsSpan(offset).CopyTo(last);
        counter += (ulong)(data.Length - offset);
        Blake2bCore.Compress(kernel, state, last, counter, last: true);

        byte[] output = new byte[Blake2bCore.StateWords * sizeof(ulong)];
        for (int i = 0; i < Blake2bCore.StateWords; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(i * sizeof(ulong)), state[i]);

        return output[..digestLength];
    }

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static Blake2bCore.KernelKind ParseSupportedKernel(string name)
    {
        Blake2bCore.KernelKind kernel = Enum.Parse<Blake2bCore.KernelKind>(name);
        if (!Blake2bCore.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
