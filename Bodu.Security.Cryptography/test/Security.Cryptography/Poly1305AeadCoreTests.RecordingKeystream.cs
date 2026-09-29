// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.RecordingKeystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Runs one framing over a seeded message with a <see cref="RecordingKeystream" /> planned for a kernel, and
    /// returns what it wrote and the draws it made.
    /// </summary>
    /// <param name="framing">
    /// The name of the framing: <c>SealRfc8439</c>, <c>OpenRfc8439</c>, <c>SealSecretbox</c> or <c>OpenSecretbox</c>.
    /// </param>
    /// <param name="kernel">The kernel the keystream plans for.</param>
    /// <param name="length">The length of the message, in bytes.</param>
    /// <returns>
    /// The bytes the framing wrote, the bytes the same framing writes drawing on a ChaCha20 engine, and the draws.
    /// </returns>
    private static (byte[] Actual, byte[] Expected, List<int> Draws) RunPlanned(string framing, ChaCha20Core.KernelKind kernel, int length)
    {
        var random = new Random(0x5EA1_0100 + length);
        byte[] key = NextBytes(random, ChaCha20Core.KeyBytes);
        byte[] nonce = NextBytes(random, ChaCha20Core.NonceBytes);
        byte[] associatedData = NextBytes(random, length % 37);
        byte[] message = NextBytes(random, length);
        byte[] sealedMessage = new byte[length + Poly1305AeadCore.TagBytes];
        var keystream = new RecordingKeystream(kernel, key, nonce);
        using var engine = new ChaCha20StreamCipher(key, nonce, initialCounter: 0);

        switch (framing)
        {
            case nameof(Poly1305AeadCore.SealRfc8439):
                byte[] expectedRfc8439 = new byte[sealedMessage.Length];
                _ = Poly1305AeadCore.SealRfc8439(engine, associatedData, message, expectedRfc8439);
                _ = Poly1305AeadCore.SealRfc8439(ref keystream, associatedData, message, sealedMessage);
                return (sealedMessage, expectedRfc8439, keystream.Draws);

            case nameof(Poly1305AeadCore.OpenRfc8439):
                byte[] openedRfc8439 = new byte[length];
                _ = Poly1305AeadCore.SealRfc8439(engine, associatedData, message, sealedMessage);
                _ = Poly1305AeadCore.OpenRfc8439(ref keystream, associatedData, sealedMessage, openedRfc8439);
                return (openedRfc8439, message, keystream.Draws);

            case nameof(Poly1305AeadCore.SealSecretbox):
                byte[] expectedSecretbox = new byte[sealedMessage.Length];
                _ = Poly1305AeadCore.SealSecretbox(engine, message, expectedSecretbox);
                _ = Poly1305AeadCore.SealSecretbox(ref keystream, message, sealedMessage);
                return (sealedMessage, expectedSecretbox, keystream.Draws);

            case nameof(Poly1305AeadCore.OpenSecretbox):
                byte[] openedSecretbox = new byte[length];
                _ = Poly1305AeadCore.SealSecretbox(engine, message, sealedMessage);
                _ = Poly1305AeadCore.OpenSecretbox(ref keystream, sealedMessage, openedSecretbox);
                return (openedSecretbox, message, keystream.Draws);

            default:
                throw new ArgumentOutOfRangeException(nameof(framing));
        }
    }

    /// <summary>
    /// Returns the number of keystream bytes before a framing's message: 64 under RFC 8439, 32 under secretbox.
    /// </summary>
    /// <param name="framing">The name of the framing.</param>
    /// <returns>The offset, in bytes.</returns>
    private static int KeystreamOffset(string framing) =>
        framing.EndsWith("Secretbox", StringComparison.Ordinal) ? 32 : ChaCha20Core.BlockBytes;

    /// <summary>
    /// Returns the draws that take a message's keystream as it comes: the block that keys Poly1305, then the rest of
    /// the message's whole blocks in one run, then its last partial block alone.
    /// </summary>
    /// <param name="offset">The number of keystream bytes before the message.</param>
    /// <param name="length">The length of the message, in bytes.</param>
    /// <returns>The draws, recorded as <see cref="RecordingKeystream.Draws" /> records them.</returns>
    private static List<int> DrawsAsItComes(int offset, int length)
    {
        List<int> draws = [0];
        int rest = Math.Max(0, offset + length - ChaCha20Core.BlockBytes);

        if (rest >= ChaCha20Core.BlockBytes)
            draws.Add(rest / ChaCha20Core.BlockBytes);

        if (rest % ChaCha20Core.BlockBytes != 0)
            draws.Add(0);

        return draws;
    }

    /// <summary>
    /// Estimates what a sequence of draws costs on a kernel, in the units of <see cref="ChaCha20Core.StepCost" />.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="draws">The draws, recorded as <see cref="RecordingKeystream.Draws" /> records them.</param>
    /// <returns>The estimated cost.</returns>
    private static int CostOf(ChaCha20Core.KernelKind kernel, List<int> draws) =>
        draws.Sum(blocks => blocks == 0 ? ChaCha20Core.StepCost(kernel, 1) : ChaCha20Core.CostFor(kernel, blocks));

    /// <summary>
    /// Asserts that a framing, with its draws planned for a kernel, writes what it writes drawing on a ChaCha20 engine,
    /// for every length in <see cref="MessageLengths" />.
    /// </summary>
    /// <param name="framing">The name of the framing.</param>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    private static void AssertPlannedMatchesTheEngine(string framing, string kernel)
    {
        var kind = Enum.Parse<ChaCha20Core.KernelKind>(kernel);

        foreach (int length in MessageLengths)
        {
            (byte[] actual, byte[] expected, _) = RunPlanned(framing, kind, length);

            CollectionAssert.AreEqual(expected, actual, $"length {length}");
        }
    }

    /// <summary>
    /// Asserts that a framing's draws, planned for a kernel, are estimated to cost less than drawing the keystream as
    /// it comes wherever they depart from it, for every length in <see cref="MessageLengths" />.
    /// </summary>
    /// <param name="framing">The name of the framing.</param>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    private static void AssertPlannedCostsNoMoreThanAsItComes(string framing, string kernel)
    {
        var kind = Enum.Parse<ChaCha20Core.KernelKind>(kernel);
        int offset = KeystreamOffset(framing);

        foreach (int length in MessageLengths)
        {
            List<int> draws = RunPlanned(framing, kind, length).Draws;
            List<int> asItComes = DrawsAsItComes(offset, length);

            if (!draws.SequenceEqual(asItComes))
            {
                Assert.IsTrue(
                    CostOf(kind, draws) < CostOf(kind, asItComes),
                    $"length {length}: drew [{string.Join(", ", draws)}] where [{string.Join(", ", asItComes)}] costs no more.");
            }
        }
    }

    /// <summary>
    /// Asserts that a framing's draws, planned for a kernel, cost what the plan estimates: the one-pass run
    /// <see cref="Poly1305AeadCore.OnePassBlocks" /> returns, or the key block and
    /// <see cref="Poly1305AeadCore.KeystreamCost" /> for the rest, for every length in <see cref="MessageLengths" />.
    /// </summary>
    /// <param name="framing">The name of the framing.</param>
    /// <param name="kernel">The name of the kernel the draws are planned for.</param>
    private static void AssertPlannedCostsWhatThePlanEstimates(string framing, string kernel)
    {
        var kind = Enum.Parse<ChaCha20Core.KernelKind>(kernel);
        int offset = KeystreamOffset(framing);

        foreach (int length in MessageLengths)
        {
            int keystreamBytes = offset + length;
            int runBlocks = Poly1305AeadCore.OnePassBlocks(kind, keystreamBytes);
            int estimate = runBlocks != 0
                ? ChaCha20Core.CostFor(kind, runBlocks)
                : ChaCha20Core.StepCost(kind, 1) + Poly1305AeadCore.KeystreamCost(kind, Math.Max(0, keystreamBytes - ChaCha20Core.BlockBytes));

            List<int> draws = RunPlanned(framing, kind, length).Draws;

            Assert.AreEqual(estimate, CostOf(kind, draws), $"length {length}: drew [{string.Join(", ", draws)}].");
        }
    }

    /// <summary>
    /// Provides a ChaCha20 keystream that reports a chosen kernel, so that the framings plan their draws for it, and
    /// records every draw they make.
    /// </summary>
    /// <remarks>
    /// The keystream itself comes from a <see cref="ChaCha20Core.Keystream" /> on the kernel dispatch selects, so it is
    /// the same whichever kernel the framings plan for: every kernel's plan can be driven, and its draws checked, on
    /// any processor.
    /// </remarks>
    private struct RecordingKeystream
        : IKeystreamSource
    {
        /// <summary>The keystream the draws are served from.</summary>
        private ChaCha20Core.Keystream _keystream;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingKeystream" /> struct at block counter 0.
        /// </summary>
        /// <param name="kernel">The kernel the keystream reports.</param>
        /// <param name="key">The 32-byte ChaCha20 key.</param>
        /// <param name="nonce">The 12-byte nonce.</param>
        internal RecordingKeystream(ChaCha20Core.KernelKind kernel, byte[] key, byte[] nonce)
        {
            Kernel = kernel;
            Draws = [];
            _keystream = default;
            _keystream.Initialize(key, nonce, counter: 0);
        }

        /// <inheritdoc />
        public ChaCha20Core.KernelKind Kernel { get; }

        /// <summary>
        /// Gets the draws made so far, in order: 0 for a block drawn alone, and the number of blocks for a run.
        /// </summary>
        internal List<int> Draws { get; }

        /// <inheritdoc />
        public void NextBlock(Span<byte> destination)
        {
            Draws.Add(0);
            _keystream.NextBlock(destination);
        }

        /// <inheritdoc />
        public void XorBlocks(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Draws.Add(input.Length / ChaCha20Core.BlockBytes);
            _keystream.XorBlocks(input, output);
        }
    }
}
