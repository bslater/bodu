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
}
