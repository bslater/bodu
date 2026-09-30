// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CtrModeTransformTests
{
    /// <summary>
    /// Verifies that CTR increments the counter in big-endian order (rightmost byte first) per
    /// NIST SP 800-38A Section 6.5. Uses an identity cipher so E(x) = x, making the keystream
    /// equal to the successive counter values.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldXorWithIncrementingCounterKeystream()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
        byte[] initialCounter = new byte[ExpectedBlockSize]; // all zeros
        CtrModeTransform transform = CreateTransform(cipher, (byte[])initialCounter.Clone());

        byte[] plaintext = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize * 2).ToArray();
        byte[] output = new byte[plaintext.Length];

        transform.Transform(plaintext, output, encrypt: true);

        // NIST big-endian increment: rightmost byte first.
        //   keystream_0 = [0, 0, …, 0]
        //   keystream_1 = [0, 0, …, 0, 1]  (last byte incremented)
        byte[] keystream0 = new byte[ExpectedBlockSize];
        byte[] keystream1 = new byte[ExpectedBlockSize];
        keystream1[ExpectedBlockSize - 1] = 1;

        byte[] exp0 = plaintext[..ExpectedBlockSize].Zip(keystream0, (a, b) => (byte)(a ^ b)).ToArray();
        byte[] exp1 = plaintext[ExpectedBlockSize..].Zip(keystream1, (a, b) => (byte)(a ^ b)).ToArray();

        CollectionAssert.AreEqual(exp0, output[..ExpectedBlockSize].ToArray(),
            "First CTR block did not match expected counter keystream.");
        CollectionAssert.AreEqual(exp1, output[ExpectedBlockSize..].ToArray(),
            "Second CTR block must reflect big-endian counter increment.");
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, EncryptAndDecrypt, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_EncryptAndDecrypt_ShouldBeSymmetric()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] counter = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i * 3)).ToArray();

        CtrModeTransform encrypt = CreateTransform(cipher, (byte[])counter.Clone());
        CtrModeTransform decrypt = CreateTransform(cipher, (byte[])counter.Clone());
        byte[] plaintext = Enumerable.Range(0, ExpectedBlockSize * 3).Select(i => (byte)i).ToArray();
        byte[] ct = new byte[plaintext.Length];
        byte[] recovered = new byte[plaintext.Length];

        encrypt.Transform(plaintext, ct, encrypt: true);
        decrypt.Transform(ct, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, when Encrypting, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenEncrypting_ShouldNotMutateInitialCounter()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] initialCounter = Enumerable.Repeat((byte)0x99, ExpectedBlockSize).ToArray();
        byte[] counterCopy = (byte[])initialCounter.Clone();
        CtrModeTransform transform = CreateTransform(cipher, initialCounter);

        transform.Transform(new byte[ExpectedBlockSize * 2], new byte[ExpectedBlockSize * 2], encrypt: true);

        CollectionAssert.AreEqual(counterCopy, initialCounter,
            "CTR must not mutate the caller-supplied initial counter array.");
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, when Decrypting, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        CtrModeTransform transform = CreateTransform(cipher, new byte[ExpectedBlockSize]);
        transform.Transform(new byte[ExpectedBlockSize * 3], new byte[ExpectedBlockSize * 3], encrypt: false);
        Assert.AreEqual(3, cipher.EncryptBlockCount, "CTR must use encrypt primitive for decryption.");
        Assert.AreEqual(0, cipher.DecryptBlockCount, "CTR must never call decrypt primitive.");
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, when CalledTwice, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Transform_WhenCalledTwice_ShouldContinueCounterAcrossCalls()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] ic = new byte[ExpectedBlockSize];
        CtrModeTransform single = CreateTransform(cipher, (byte[])ic.Clone());
        CtrModeTransform streamed = CreateTransform(cipher, (byte[])ic.Clone());
        byte[] pt = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
        byte[] sOut = new byte[pt.Length];
        byte[] dOut = new byte[pt.Length];

        single.Transform(pt, sOut, encrypt: true);
        streamed.Transform(pt.AsSpan(0, ExpectedBlockSize), dOut.AsSpan(0, ExpectedBlockSize), encrypt: true);
        streamed.Transform(pt.AsSpan(ExpectedBlockSize), dOut.AsSpan(ExpectedBlockSize), encrypt: true);

        CollectionAssert.AreEqual(sOut, dOut, "CTR must preserve counter across successive calls.");
    }

    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" />, with DifferentInitialCounters, returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void Transform_WithDifferentInitialCounters_ShouldProduceDifferentCiphertext()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] counterA = new byte[ExpectedBlockSize];
        byte[] counterB = new byte[ExpectedBlockSize];
        counterB[ExpectedBlockSize - 1] = 0x80;

        CtrModeTransform a = CreateTransform(cipher, counterA);
        CtrModeTransform b = CreateTransform(cipher, counterB);
        byte[] pt = new byte[ExpectedBlockSize];
        byte[] oA = new byte[ExpectedBlockSize];
        byte[] oB = new byte[ExpectedBlockSize];

        a.Transform(pt, oA, encrypt: true);
        b.Transform(pt, oB, encrypt: true);

        CollectionAssert.AreNotEqual(oA, oB);
    }

    /// <summary>
    /// Verifies that the keystream matches a block-at-a-time reference — one counter block encrypted per whole or
    /// partial block of each call, the rest of a partial block's keystream discarded — for messages that fit in one
    /// run of counters, straddle one, and span several, and for calls split both on and off block boundaries.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    [TestMethod]
    [DynamicData(nameof(BatchingCiphers))]
    public void Transform_WhenInputSpansSeveralRunsOfCounters_ShouldMatchBlockAtATimeReference(string name, Func<IBlockCipher> create)
    {
        int[][] callPlans =
        [
            [0], [1], [15], [16], [17], [4095], [4096], [4097], [(3 * 4096) + 17], [20000],
            [7, 4096 + 5, 11], [64, 64, 64], [4096, 4096], [1, 1, 1, 8191],
        ];

        foreach (int[] calls in callPlans)
        {
            using IBlockCipher cipher = create();
            int blockSize = cipher.BlockSize / 8;
            byte[] initialCounter = new byte[blockSize];
            initialCounter[^1] = 0xF0;
            initialCounter[^2] = 0xFF;

            byte[] input = new byte[calls.Sum()];
            new Random(input.Length).NextBytes(input);

            byte[] expected = ReferenceKeystreamXor(cipher, initialCounter, input, calls);

            byte[] actual = new byte[input.Length];
            using var transform = new CtrModeTransform(cipher, initialCounter);
            int offset = 0;
            foreach (int length in calls)
            {
                transform.Transform(input.AsSpan(offset, length), actual.AsSpan(offset, length), encrypt: true);
                offset += length;
            }

            CollectionAssert.AreEqual(expected, actual, $"{name}, calls {string.Join("+", calls)}");
        }
    }

    /// <summary>
    /// Verifies that a message split into two calls at every byte gives the output of a block-at-a-time reference, so
    /// each call's whole blocks, its partial last block and the counter it hands on agree wherever the split falls.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    /// <remarks>
    /// The message is long enough to reach a group of eight blocks, a group of four and single blocks on either side of
    /// every split, for ciphers that run their counters in groups.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(BatchingCiphers))]
    public void Transform_WhenSplitIntoTwoCallsAtEveryByte_ShouldMatchBlockAtATimeReference(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        int blockSize = cipher.BlockSize / 8;
        byte[] initialCounter = new byte[blockSize];
        initialCounter[^1] = 0xF7;

        byte[] input = new byte[(16 * 13) + 7];
        new Random(0x5E17).NextBytes(input);

        for (int split = 0; split <= input.Length; split++)
        {
            int[] calls = [split, input.Length - split];
            byte[] expected = ReferenceKeystreamXor(cipher, initialCounter, input, calls);

            byte[] actual = new byte[input.Length];
            using var transform = new CtrModeTransform(cipher, initialCounter);
            transform.Transform(input.AsSpan(0, split), actual.AsSpan(0, split), encrypt: true);
            transform.Transform(input.AsSpan(split), actual.AsSpan(split), encrypt: true);

            CollectionAssert.AreEqual(expected, actual, $"{name}, split at {split}");
        }
    }

    /// <summary>
    /// Verifies that the counter carries out of its last four, and its last eight, bytes as a block-at-a-time reference
    /// carries it: the carry falls inside the first group of blocks, and the message runs on for several groups.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    /// <remarks>
    /// For a 16-byte block these are the carries out of the counter's lowest 32-bit word and out of its lower 64-bit
    /// half, which a cipher forming its counters a word at a time must propagate.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(BatchingCiphers))]
    public void Transform_WhenCounterCarriesOutOfItsLowWords_ShouldMatchBlockAtATimeReference(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        int blockSize = cipher.BlockSize / 8;
        byte[] input = new byte[(16 * 40) + 3];
        new Random(0x5E18).NextBytes(input);

        foreach (int carryBytes in new[] { 4, 8 })
        {
            if (carryBytes >= blockSize)
                continue;

            byte[] initialCounter = new byte[blockSize];
            initialCounter[0] = 0x5A;
            initialCounter.AsSpan(blockSize - carryBytes).Fill(0xFF);
            initialCounter[^1] = 0xFA;

            byte[] expected = ReferenceKeystreamXor(cipher, initialCounter, input, [input.Length]);

            byte[] actual = new byte[input.Length];
            using var transform = new CtrModeTransform(cipher, initialCounter);
            transform.Transform(input, actual, encrypt: true);

            CollectionAssert.AreEqual(expected, actual, $"{name}, carry out of the last {carryBytes} bytes");
        }
    }
}
