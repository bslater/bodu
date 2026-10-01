// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

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

    /// <summary>RFC 7914, Section 8's Salsa20/8 core input.</summary>
    private const string SalsaInputHex =
        "7e879a214f3ec9867ca940e641718f26baee555b8c61c1b50df846116dcd3b1d" +
        "ee24f319df9b3d8514121e4b5ac5aa3276021d2909c74829edebc68db8b8c25e";

    /// <summary>RFC 7914, Section 8's Salsa20/8 core output.</summary>
    private const string SalsaOutputHex =
        "a41f859c6608cc993b81cacb020cef05044b2181a2fd337dfd7b1c6396682f29" +
        "b4393168e3c9e6bcfe6bc5b7a06d96bae424cc102c91745c24ad673dc7618f81";

    /// <summary>RFC 7914, Section 9's scryptBlockMix output for <see cref="RomixInputHex" />, with <c>r = 1</c>.</summary>
    private const string BlockMixOutputHex =
        "a41f859c6608cc993b81cacb020cef05044b2181a2fd337dfd7b1c6396682f29" +
        "b4393168e3c9e6bcfe6bc5b7a06d96bae424cc102c91745c24ad673dc7618f81" +
        "20edc975323881a80540f64c162dcd3c21077cfe5f8d5fe2b1a4168f953678b7" +
        "7d3b3d803b60e4ab920996e59b4d53b65d2a225877d5edf5842cb9f14eefe425";

    /// <summary>An idle timeout long enough that a pool's own timer never fires during a test.</summary>
    private static readonly TimeSpan LongIdleTimeout = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the light OpenSSL corpus rows with more than one unit, the rows a derivation can divide among threads.
    /// </summary>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> OpenSslCorpusWithSeveralUnits() =>
        ScryptTests.OpenSslCorpusLight().Where(static row => ((KdfKnownAnswer)row[0]).Parallelism > 1);

    /// <summary>
    /// Derives RFC 7914, Section 12's second vector - sixteen units of 1 MiB - as the options describe.
    /// </summary>
    /// <param name="options">How the derivation runs its units.</param>
    /// <returns>The derived key, as lowercase hex.</returns>
    private static string DeriveSecondRfc7914Key(ScryptCore.MixOptions options)
    {
        byte[] key = new byte[64];
        ScryptCore.DeriveKey(Encoding.ASCII.GetBytes("password"), Encoding.ASCII.GetBytes("NaCl"), 1024, 8, 16, key, options);
        return Convert.ToHexString(key).ToLowerInvariant();
    }

    /// <summary>
    /// Derives RFC 7914, Section 12's second vector on the calling thread, with the workspace taken from the specified
    /// pool.
    /// </summary>
    /// <param name="pool">The pool the derivation takes its workspace from.</param>
    /// <returns>The derived key, as lowercase hex.</returns>
    private static string DeriveSecondRfc7914Key(NativeBufferPool pool) =>
        DeriveSecondRfc7914Key(new ScryptCore.MixOptions(1, pool: pool));

    /// <summary>
    /// Creates a pool that retains up to the specified number of buffers of up to 64 MiB, released after an hour idle.
    /// </summary>
    /// <param name="maxRetainedBuffers">The greatest number of buffers the pool retains.</param>
    /// <returns>The pool.</returns>
    private static NativeBufferPool CreatePool(int maxRetainedBuffers = 4) =>
        new(maxRetainedBuffers, 64L * 1024 * 1024, LongIdleTimeout, TimeProvider.System);

    /// <summary>
    /// Returns the operations of the named kernel, or reports the test inconclusive when the processor cannot run it.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <returns>The kernel's operations, taking and returning blocks in RFC 7914's word order.</returns>
    private static KernelOperations OperationsOf(string kernel) =>
        ParseSupportedKernel(kernel) switch
        {
            ScryptCore.KernelKind.Sse2 => OperationsOf<ScryptCore.Vector128Kernel<ScryptCore.Sse2Isa>>(),
            ScryptCore.KernelKind.AdvSimd => OperationsOf<ScryptCore.Vector128Kernel<ScryptCore.AdvSimdIsa>>(),
            _ => OperationsOf<ScryptCore.ScalarKernel>(),
        };

    /// <summary>
    /// Returns the operations of a kernel, each converting its blocks into the kernel's word order and back.
    /// </summary>
    /// <typeparam name="TKernel">The kernel.</typeparam>
    /// <returns>The kernel's operations.</returns>
    private static KernelOperations OperationsOf<TKernel>()
        where TKernel : struct, ScryptCore.IScryptKernel =>
        new(
            block =>
            {
                TKernel.Import(ref block[0], 1);
                TKernel.Salsa20_8(ref block[0]);
                TKernel.Export(ref block[0], 1);
            },
            (input, output, blockSizeR) =>
            {
                uint[] source = (uint[])input.Clone();
                TKernel.Import(ref source[0], 2 * blockSizeR);
                TKernel.BlockMix(ref source[0], ref output[0], blockSizeR);
                TKernel.Export(ref output[0], 2 * blockSizeR);
            },
            (x, v, output, blockSizeR) =>
            {
                uint[] first = (uint[])x.Clone();
                uint[] second = (uint[])v.Clone();
                TKernel.Import(ref first[0], 2 * blockSizeR);
                TKernel.Import(ref second[0], 2 * blockSizeR);
                TKernel.BlockMixXor(ref first[0], ref second[0], ref output[0], blockSizeR);
                TKernel.Export(ref output[0], 2 * blockSizeR);
            });

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static ScryptCore.KernelKind ParseSupportedKernel(string name)
    {
        ScryptCore.KernelKind kernel = Enum.Parse<ScryptCore.KernelKind>(name);
        if (!ScryptCore.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }

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
