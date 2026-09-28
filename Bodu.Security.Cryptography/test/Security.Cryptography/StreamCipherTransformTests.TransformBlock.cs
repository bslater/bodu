// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherTransformTests.TransformBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class StreamCipherTransformTests
{
    /// <summary>
    /// Verifies that a message split into two <see cref="StreamCipherTransform.TransformBlock" /> calls and a
    /// <see cref="StreamCipherTransform.TransformFinalBlock" /> call, at split points on and around block boundaries,
    /// is combined with the keystream a single engine produces one block at a time.
    /// </summary>
    /// <param name="cipher">The cipher.</param>
    [TestMethod]
    [DataRow("ChaCha20")]
    [DataRow("Salsa20")]
    public void TransformBlock_WhenInputIsSplitAnywhere_ShouldMatchSingleBlockKeystream(string cipher)
    {
        var random = new Random(0x57C1_0001);
        byte[] input = new byte[(21 * 64) + 29];
        random.NextBytes(input);
        using IStreamCipher reference = CreateEngine(cipher, 7, 3);
        byte[] expected = XorBlockByBlock(reference, input);

        foreach (int first in new[] { 0, 1, 5, 63, 64, 65, 127, 200, 255, 256, 700 })
        {
            foreach (int second in new[] { 0, 1, 17, 64, 511, 512, 513 })
            {
                using var transform = new StreamCipherTransform(CreateEngine(cipher, 7, 3));
                byte[] actual = new byte[input.Length];

                transform.TransformBlock(input, 0, first, actual, 0);
                transform.TransformBlock(input, first, second, actual, first);
                byte[] last = transform.TransformFinalBlock(input, first + second, input.Length - first - second);
                last.CopyTo(actual, first + second);

                CollectionAssert.AreEqual(expected, actual, $"split at {first} and {first + second}");
            }
        }
    }

    /// <summary>
    /// Verifies that a transform whose engine's counter wraps from its largest value to zero within one call combines
    /// the input with the keystream a single engine produces one block at a time across the wrap.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenCounterWrapsWithinTheInput_ShouldMatchSingleBlockKeystream()
    {
        var random = new Random(0x57C1_0002);
        byte[] input = new byte[(20 * 64) + 13];
        random.NextBytes(input);
        using IStreamCipher reference = CreateEngine("ChaCha20", 11, uint.MaxValue - 5);
        byte[] expected = XorBlockByBlock(reference, input);
        using var transform = new StreamCipherTransform(CreateEngine("ChaCha20", 11, uint.MaxValue - 5));
        byte[] actual = new byte[input.Length];

        transform.TransformBlock(input, 0, input.Length, actual, 0);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a transform over an engine without a bulk entry point, which takes its one-block-at-a-time path,
    /// produces the same output as a transform over the bulk engine.
    /// </summary>
    /// <param name="cipher">The cipher.</param>
    [TestMethod]
    [DataRow("ChaCha20")]
    [DataRow("Salsa20")]
    public void TransformBlock_WhenEngineHasNoBulkPath_ShouldMatchTheBulkEngine(string cipher)
    {
        var random = new Random(0x57C1_0003);
        byte[] input = new byte[(33 * 64) + 5];
        random.NextBytes(input);
        using var bulk = new StreamCipherTransform(CreateEngine(cipher, 13, 9));
        using var single = new StreamCipherTransform(new SingleBlockStreamCipher(CreateEngine(cipher, 13, 9)));
        byte[] expected = new byte[input.Length];
        byte[] actual = new byte[input.Length];

        bulk.TransformBlock(input, 0, 100, expected, 0);
        bulk.TransformBlock(input, 100, input.Length - 100, expected, 100);
        single.TransformBlock(input, 0, 100, actual, 0);
        single.TransformBlock(input, 100, input.Length - 100, actual, 100);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that transforming in place, with the output the input itself, matches transforming into a separate
    /// buffer.
    /// </summary>
    /// <param name="cipher">The cipher.</param>
    [TestMethod]
    [DataRow("ChaCha20")]
    [DataRow("Salsa20")]
    public void TransformBlock_WhenOutputIsTheInput_ShouldMatchSeparateOutput(string cipher)
    {
        var random = new Random(0x57C1_0004);
        byte[] input = new byte[(19 * 64) + 45];
        random.NextBytes(input);
        using var separate = new StreamCipherTransform(CreateEngine(cipher, 17, 0));
        using var inPlace = new StreamCipherTransform(CreateEngine(cipher, 17, 0));
        byte[] expected = new byte[input.Length];
        byte[] buffer = (byte[])input.Clone();

        separate.TransformBlock(input, 0, 70, expected, 0);
        separate.TransformBlock(input, 70, input.Length - 70, expected, 70);
        inPlace.TransformBlock(buffer, 0, 70, buffer, 0);
        inPlace.TransformBlock(buffer, 70, input.Length - 70, buffer, 70);

        CollectionAssert.AreEqual(expected, buffer);
    }
}
