// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconXof128" /> extendable output function.
/// Known-answer test vectors are in <c>AsconXof128Tests.KnownAnswerTests.cs</c>.
/// </summary>
[TestClass]
public partial class AsconXof128Tests
{
    // ── AlgorithmName ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconXof128.AlgorithmName" /> returns the canonical NIST SP 800-232
    /// algorithm identifier.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_ShouldReturnAsconXof128()
    {
        using AsconXof128 sut = new AsconXof128();
        Assert.AreEqual("ASCON-XOF128", sut.AlgorithmName);
    }

    // ── GetHash length ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconXof128.GetHash(int)" /> returns a byte array of exactly the
    /// requested length.
    /// </summary>
    [TestMethod]
    public void GetHash_ShouldReturnArrayOfRequestedLength()
    {
        using AsconXof128 sut = new AsconXof128();
        sut.Absorb([0x01, 0x02, 0x03]);

        byte[] output = sut.GetHash(64);

        Assert.AreEqual(64, output.Length);
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.GetHash(int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the requested length is zero.
    /// </summary>
    [TestMethod]
    public void GetHash_WhenLengthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using AsconXof128 sut = new AsconXof128();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetHash(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.GetHash(int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the requested length is negative.
    /// </summary>
    [TestMethod]
    public void GetHash_WhenLengthIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using AsconXof128 sut = new AsconXof128();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetHash(-1);
        });
    }

    // ── Absorb / Squeeze state-machine ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that calling <see cref="AsconXof128.Absorb" /> after <see cref="AsconXof128.Squeeze" />
    /// has been called throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Absorb_AfterSqueeze_ShouldThrowInvalidOperationException()
    {
        using AsconXof128 sut = new AsconXof128();
        sut.Squeeze(new byte[8]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Absorb([0x01]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.Initialize" /> resets the state so that the instance
    /// can be reused for a new message after squeezing has begun.
    /// </summary>
    [TestMethod]
    public void Initialize_AfterSqueeze_ShouldAllowFreshAbsorption()
    {
        using AsconXof128 sut = new AsconXof128();
        sut.Absorb([0xFF]);
        sut.Squeeze(new byte[8]);

        sut.Initialize();

        // After reset, absorption must succeed without throwing.
        sut.Absorb([0x01]);
        byte[] output = sut.GetHash(32);
        Assert.AreEqual(32, output.Length);
    }

    // ── Dispose ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconXof128.Absorb" /> throws <see cref="ObjectDisposedException" />
    /// after the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Absorb_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconXof128 sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Absorb([0x01]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.Squeeze" /> throws <see cref="ObjectDisposedException" />
    /// after the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Squeeze_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconXof128 sut = new AsconXof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Squeeze(new byte[8]);
        });
    }

    // ── Determinism and correctness ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that squeezing the same number of bytes across multiple calls returns the same
    /// bytes as squeezing them all at once, confirming the streaming output is consistent.
    /// </summary>
    [TestMethod]
    public void Squeeze_InMultipleCallsVsOneCall_ShouldProduceIdenticalOutput()
    {
        byte[] message = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // One shot: squeeze all 24 bytes at once.
        using AsconXof128 oneShot = new AsconXof128();
        oneShot.Absorb(message);
        byte[] single = new byte[24];
        oneShot.Squeeze(single);

        // Incremental: squeeze 7 + 9 + 8 bytes separately.
        using AsconXof128 incr = new AsconXof128();
        incr.Absorb(message);
        byte[] part1 = new byte[7];
        byte[] part2 = new byte[9];
        byte[] part3 = new byte[8];
        incr.Squeeze(part1);
        incr.Squeeze(part2);
        incr.Squeeze(part3);
        byte[] incremental = [.. part1, .. part2, .. part3];

        CollectionAssert.AreEqual(single, incremental,
            "Squeezing in parts must produce the same output as squeezing all at once.");
    }

    /// <summary>
    /// Verifies that absorbing data incrementally produces the same output as absorbing it in
    /// a single call.
    /// </summary>
    [TestMethod]
    public void Absorb_IncrementalVsSingleCall_ShouldProduceIdenticalOutput()
    {
        byte[] message = new byte[25];
        for (int i = 0; i < message.Length; i++) message[i] = (byte)i;

        using AsconXof128 single = new AsconXof128();
        single.Absorb(message);
        byte[] outputSingle = single.GetHash(32);

        using AsconXof128 incr = new AsconXof128();
        incr.Absorb(message.AsSpan(0, 10));
        incr.Absorb(message.AsSpan(10, 8));
        incr.Absorb(message.AsSpan(18));
        byte[] outputIncr = incr.GetHash(32);

        CollectionAssert.AreEqual(outputSingle, outputIncr,
            "Incremental absorption must produce the same output as a single Absorb call.");
    }

    /// <summary>
    /// Verifies that the output of <see cref="AsconXof128.HashData" /> matches the output of
    /// using instance methods for the same input.
    /// </summary>
    [TestMethod]
    public void HashData_ShouldMatchInstanceMethodOutput()
    {
        byte[] input = [0xCA, 0xFE, 0xBA, 0xBE];

        using AsconXof128 inst = new AsconXof128();
        inst.Absorb(input);
        byte[] instanceOutput = inst.GetHash(32);

        byte[] staticOutput = AsconXof128.HashData(input, 32);

        CollectionAssert.AreEqual(instanceOutput, staticOutput,
            "HashData static method must produce the same output as the instance API for the same input.");
    }

    /// <summary>
    /// Verifies that different inputs produce different outputs (collision sanity check).
    /// </summary>
    [TestMethod]
    public void GetHash_ForDifferentInputs_ShouldProduceDifferentOutputs()
    {
        byte[] output1 = AsconXof128.HashData([0x01], 32);
        byte[] output2 = AsconXof128.HashData([0x02], 32);

        CollectionAssert.AreNotEqual(output1, output2,
            "Two different inputs must not produce the same 256-bit XOF output.");
    }

    /// <summary>
    /// Verifies that the empty-message output differs from the output for a non-empty message.
    /// </summary>
    [TestMethod]
    public void GetHash_EmptyInputDiffersFromNonEmpty()
    {
        byte[] empty    = AsconXof128.HashData([], 32);
        byte[] nonEmpty = AsconXof128.HashData([0x00], 32);

        CollectionAssert.AreNotEqual(empty, nonEmpty,
            "Empty-message output must differ from a single zero-byte message.");
    }
}
