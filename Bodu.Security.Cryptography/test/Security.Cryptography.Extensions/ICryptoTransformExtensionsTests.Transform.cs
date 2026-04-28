// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions;

public partial class ICryptoTransformExtensionsTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Shared test data
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns known-answer test cases for <see cref="SimpleReversingCryptoTransform" /> operating
    /// in ECB mode with a 128-bit block size, an all-zero key, and no padding.
    /// </summary>
    /// <remarks>
    /// All expected outputs are independently derived: reversing a 16-byte block is trivially
    /// verifiable by inspection, so no external reference implementation is required. The
    /// <c>CipherMode.ECB</c> setting means each block is transformed in isolation — no IV chaining
    /// — making per-block verification straightforward.
    /// </remarks>
    public static IEnumerable<object[]> GetValidTransformTestData()
    {
        // ── Single-block encrypt ─────────────────────────────────────────────────────────────────

        // Sequential ascending bytes — byte-by-byte reversal is immediately verifiable.
        yield return KatRow("Sequential ascending / encrypt",
            input: "000102030405060708090A0B0C0D0E0F",
            expected: "0F0E0D0C0B0A09080706050403020100",
            mode: TransformMode.Encrypt);

        // All-zero block — fixed point: reverse(0x00…) == 0x00…
        yield return KatRow("All zeros / encrypt (fixed point)",
            input: "00000000000000000000000000000000",
            expected: "00000000000000000000000000000000",
            mode: TransformMode.Encrypt);

        // All-0xFF block — fixed point: reverse(0xFF…) == 0xFF…
        yield return KatRow("All 0xFF / encrypt (fixed point)",
            input: "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
            expected: "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
            mode: TransformMode.Encrypt);

        // Alternating 0xAA/0x55 — confirms adjacent byte positions are independently swapped.
        yield return KatRow("Alternating 0xAA 0x55 / encrypt",
            input: "AA55AA55AA55AA55AA55AA55AA55AA55",
            expected: "55AA55AA55AA55AA55AA55AA55AA55AA",
            mode: TransformMode.Encrypt);

        // Mixed real-world-style pattern — exercises every nibble value.
        yield return KatRow("Mixed pattern / encrypt",
            input: "DEADBEEFCAFEBABE0102030405060708",
            expected: "0807060504030201BEBAFECAEFBEADDE",
            mode: TransformMode.Encrypt);

        // ── Multi-block encrypt ──────────────────────────────────────────────────────────────────

        // Two full blocks — verifies each block is reversed independently (ECB isolation).
        yield return KatRow("Two blocks / encrypt",
            input: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            expected: "0F0E0D0C0B0A090807060504030201001F1E1D1C1B1A19181716151413121110",
            mode: TransformMode.Encrypt);

        // Three full blocks — exercises a third independent reversal and confirms no
        // carry-over between blocks in ECB mode.
        yield return KatRow("Three blocks / encrypt",
            input: "000102030405060708090A0B0C0D0E0F" +
                      "101112131415161718191A1B1C1D1E1F" +
                      "202122232425262728292A2B2C2D2E2F",
            expected: "0F0E0D0C0B0A09080706050403020100" +
                      "1F1E1D1C1B1A19181716151413121110" +
                      "2F2E2D2C2B2A29282726252423222120",
            mode: TransformMode.Encrypt);

        // ── Decrypt ──────────────────────────────────────────────────────────────────────────────
        // SimpleReversingBlockCipher is self-inverse: Decrypt(Encrypt(x)) == x.
        // Feeding the ciphertext back through Decrypt must recover the original plaintext.

        // Single-block decrypt round-trip.
        yield return KatRow("Sequential ascending / decrypt round-trip",
            input: "0F0E0D0C0B0A09080706050403020100",
            expected: "000102030405060708090A0B0C0D0E0F",
            mode: TransformMode.Decrypt);

        // Two-block decrypt round-trip.
        yield return KatRow("Two blocks / decrypt round-trip",
            input: "0F0E0D0C0B0A090807060504030201001F1E1D1C1B1A19181716151413121110",
            expected: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            mode: TransformMode.Decrypt);
    }

    // ── Factory helper ────────────────────────────────────────────────────────────────────────────

    private static object[] KatRow(
        string name,
        string input,
        string expected,
        TransformMode mode,
        PaddingMode padding = PaddingMode.None,
        int blockSizeBits = 128)
        => new object[]
        {
            new KnownAnswerTest
            {
                Name           = name,
                Input          = Convert.FromHexString(input),
                ExpectedOutput = Convert.FromHexString(expected),
                Parameters     = new Dictionary<string, object>
                {
                    ["BlockSize"] = blockSizeBits,
                    ["Padding"]   = padding,
                    ["Mode"]      = mode,
                }
            }
        };

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(byte[])
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_ByteArray_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.Transform(new byte[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_ByteArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.Transform(null!));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[])" /> returns the
    /// expected transformed output for each known-answer test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_ByteArray_WhenValid_ShouldReturnExpectedOutput(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);

        byte[] actual = transform.Transform(kat.Input);

        CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test '{kat.Name}' failed.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(byte[], int, int)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[],int,int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the offset and count combination exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Transform_ByteArrayRange_WhenOffsetPlusCountExceedsLength_ShouldThrowArgumentException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        byte[] data = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentException>(() =>
            transform.Transform(data, 2, 2));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[],int,int)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_ByteArrayRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.Transform(null!, 0, 4));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,byte[],int,int)" /> returns
    /// the expected transformed output for a valid offset and count.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_ByteArrayRange_WhenValid_ShouldReturnExpectedOutput(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);

        byte[] actual = transform.Transform(kat.Input, 0, kat.Input.Length);

        CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test '{kat.Name}' failed.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(ReadOnlySpan<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlySpan{byte})" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_Span_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.Transform((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlySpan{byte})" />
    /// returns the expected transformed output for each known-answer test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_Span_WhenValid_ShouldReturnExpectedOutput(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);

        byte[] actual = transform.Transform((ReadOnlySpan<byte>)kat.Input);

        CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test '{kat.Name}' failed.");
    }

    /// <summary>
    /// Verifies that the span overload produces output identical to the byte-array overload for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput(KnownAnswerTest kat)
    {
        byte[] fromArray;
        using (var transformA = CreateTransform(kat))
            fromArray = transformA.Transform(kat.Input);

        byte[] fromSpan;
        using (var transformB = CreateTransform(kat))
            fromSpan = transformB.Transform((ReadOnlySpan<byte>)kat.Input);

        CollectionAssert.AreEqual(fromArray, fromSpan, $"Test '{kat.Name}' produced different output.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(ReadOnlyMemory<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlyMemory{byte})" />
    /// returns the expected transformed output for each known-answer test case.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_Memory_WhenValid_ShouldReturnExpectedOutput(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);

        byte[] actual = transform.Transform(new ReadOnlyMemory<byte>(kat.Input));

        CollectionAssert.AreEqual(kat.ExpectedOutput, actual, $"Test '{kat.Name}' failed.");
    }

    /// <summary>
    /// Verifies that the memory overload produces output identical to the span overload for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput(KnownAnswerTest kat)
    {
        byte[] fromSpan;
        using (var transformA = CreateTransform(kat))
            fromSpan = transformA.Transform((ReadOnlySpan<byte>)kat.Input);

        byte[] fromMemory;
        using (var transformB = CreateTransform(kat))
            fromMemory = transformB.Transform(new ReadOnlyMemory<byte>(kat.Input));

        CollectionAssert.AreEqual(fromSpan, fromMemory, $"Test '{kat.Name}' produced different output.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(ReadOnlySpan<byte>, Span<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlySpan{byte},Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_SpanToSpan_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ReadOnlySpan<byte> input = new byte[] { 1, 2, 3, 4 };
            Span<byte> dest = new byte[64];
            transform!.Transform(input, dest);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlySpan{byte},Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination span is too small to hold the output.
    /// </summary>
    [TestMethod]
    public void Transform_SpanToSpan_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> input = new byte[] { 1, 2, 3, 4 };
            Span<byte> tooSmall = new byte[1];
            transform.Transform(input, tooSmall);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlySpan{byte},Span{byte})" />
    /// writes the transformed output into the destination span and returns the number of bytes written.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_SpanToSpan_WhenValid_ShouldWriteToDestinationAndReturnByteCount(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);
        byte[] destBuffer = new byte[kat.Input.Length + transform.OutputBlockSize];

        int written = transform.Transform((ReadOnlySpan<byte>)kat.Input, destBuffer.AsSpan());

        Assert.AreEqual(kat.ExpectedOutput.Length, written);
        CollectionAssert.AreEqual(kat.ExpectedOutput, destBuffer[..written], $"Test '{kat.Name}' failed.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(ReadOnlyMemory<byte>, Memory<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_MemoryToMemory_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            var dest = new Memory<byte>(new byte[64]);
            transform!.Transform(input, dest);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is too small to hold the output.
    /// </summary>
    [TestMethod]
    public void Transform_MemoryToMemory_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            var tooSmall = new Memory<byte>(new byte[1]);
            transform.Transform(input, tooSmall);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte})" />
    /// writes the transformed output into the destination memory region and returns the byte count.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_MemoryToMemory_WhenValid_ShouldWriteToDestinationAndReturnByteCount(KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);
        byte[] destBuffer = new byte[kat.Input.Length + transform.OutputBlockSize];

        int written = transform.Transform(
            new ReadOnlyMemory<byte>(kat.Input),
            new Memory<byte>(destBuffer));

        Assert.AreEqual(kat.ExpectedOutput.Length, written);
        CollectionAssert.AreEqual(kat.ExpectedOutput, destBuffer[..written], $"Test '{kat.Name}' failed.");
    }

    /// <summary>
    /// Verifies that the memory-to-memory overload produces output identical to the span-to-span overload for
    /// the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_MemoryToMemory_WhenComparedToSpanToSpanOverload_ShouldProduceIdenticalOutput(KnownAnswerTest kat)
    {
        byte[] spanDest = new byte[kat.Input.Length + 32];
        int spanWritten;
        using (var transformA = CreateTransform(kat))
            spanWritten = transformA.Transform((ReadOnlySpan<byte>)kat.Input, spanDest.AsSpan());

        byte[] memoryDest = new byte[kat.Input.Length + 32];
        int memoryWritten;
        using (var transformB = CreateTransform(kat))
            memoryWritten = transformB.Transform(new ReadOnlyMemory<byte>(kat.Input), new Memory<byte>(memoryDest));

        Assert.AreEqual(spanWritten, memoryWritten);
        CollectionAssert.AreEqual(spanDest[..spanWritten], memoryDest[..memoryWritten], $"Test '{kat.Name}' produced different output.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Transform(Stream, Stream, int) — synchronous stream overload
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.Transform(source, target, 16));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.Transform(null!, target, 16));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.Transform(source, null!, 16));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            transform.Transform(source, target, 0));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" />
    /// writes the transformed output to the target stream and returns the total bytes read.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void Transform_Stream_WhenValid_ShouldWriteTransformedBytesToTarget(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        using var target = new MemoryStream();
        using var transform = CreateTransform(kat);

        int bytesRead = transform.Transform(source, target, bufferSize: kat.Input.Length);

        Assert.AreEqual(kat.Input.Length, bytesRead);
        CollectionAssert.AreEqual(kat.ExpectedOutput, target.ToArray(), $"Test '{kat.Name}' failed.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" />
    /// does not dispose the target stream after completing the operation.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenCompleted_ShouldLeaveTargetStreamOpen()
    {
        using var algorithm = CreateAlgorithm();
        using var encryptor = algorithm.CreateEncryptor();
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        var target = new MemoryStream();

        encryptor.Transform(source, target, bufferSize: 16);

        Assert.IsTrue(target.CanWrite, "Target stream should remain open after Transform.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.Transform(ICryptoTransform,Stream,Stream,int)" />
    /// does not dispose the target stream when the source is empty and padding mode is
    /// <see cref="PaddingMode.None" />.
    /// </summary>
    [TestMethod]
    public void Transform_Stream_WhenSourceIsEmptyAndNoPadding_ShouldLeaveTargetStreamOpen()
    {
        using var algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;
        using var encryptor = algorithm.CreateEncryptor();
        using var source = new MemoryStream();
        var target = new MemoryStream();

        encryptor.Transform(source, target, bufferSize: algorithm.BlockSize / 8);

        Assert.IsTrue(target.CanWrite, "Target stream should remain open after Transform.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Shared factory helper
    // ---------------------------------------------------------------------------------------------------------------

    private static SimpleReversingCryptoTransform CreateTransform(KnownAnswerTest? kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        int blockSizeBits = kat.TryGet("BlockSize", out int bs) ? bs : 128;
        PaddingMode paddingMode = kat.TryGet("Padding", out PaddingMode p) ? p : PaddingMode.None;
        bool encrypt = !kat.TryGet("Mode", out TransformMode m) || m == TransformMode.Encrypt;
        byte[] iv = kat.TryGet("IV", out byte[]? ivVal) ? ivVal! : new byte[blockSizeBits / 8];
        byte[]? tweak = kat.TryGet("Tweak", out byte[]? t) ? t : null;

        // Key has no effect on output — use a deterministic all-zero key of the block size.
        byte[] key = new byte[blockSizeBits / 8];

        var cipher = tweak is null
            ? new SimpleReversingBlockCipher(key, blockSizeBits / 8)
            : new SimpleReversingBlockCipher(key, blockSizeBits / 8, tweak);

        // Use ECB so each block is transformed independently — expected outputs are simple per-block reversals.
        return new SimpleReversingCryptoTransform(cipher, CipherBlockMode.ECB, paddingMode, iv, encrypt);
    }
}
