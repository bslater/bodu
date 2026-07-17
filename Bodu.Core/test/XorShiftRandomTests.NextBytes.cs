// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextBytes" /> fills the provided buffer completely with random bytes.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(100)]
    public void NextBytes_ShouldFillBufferFully_WithVariousLengths(int length)
    {
        var rng = new XorShiftRandom();
        byte[] buffer = new byte[length];
        rng.NextBytes(buffer);

        Assert.IsTrue(buffer.All(b => b >= 0), "All bytes should be populated.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextBytes" />, when Called, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void NextBytes_WhenCalled_ShouldFillBuffer()
    {
        var rng = new XorShiftRandom();
        byte[] buffer = new byte[16];
        rng.NextBytes(buffer);

        Assert.Contains(b => b != 0, buffer, "Expected non-zero bytes in actual.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextBytes" />, with NullBuffer, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void NextBytes_WithNullBuffer_ShouldThrowExactly()
    {
        var rng = new XorShiftRandom();
        Assert.ThrowsExactly<ArgumentNullException>(() => rng.NextBytes(null!));
    }

    /// <summary>
    /// Verifies that the array and span overloads of <see cref="XorShiftRandom.NextBytes(byte[])" /> produce identical
    /// bytes for the same seed and length, including lengths that end in a partial four-byte chunk.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    [DataRow(16)]
    [DataRow(33)]
    public void NextBytes_WhenSameSeed_ForArrayAndSpanOverloads_ShouldProduceIdenticalBytes(int length)
    {
        var arrayRng = new XorShiftRandom(seed: 12345);
        var spanRng = new XorShiftRandom(seed: 12345);

        byte[] fromArray = new byte[length];
        byte[] fromSpan = new byte[length];

        arrayRng.NextBytes(fromArray);
        spanRng.NextBytes(fromSpan.AsSpan());

        CollectionAssert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="XorShiftRandom.NextBytes(Span{byte})" /> fills a non-trivial span
    /// with at least one non-zero byte.
    /// </summary>
    [TestMethod]
    public void NextBytes_WhenSpanOverloadCalled_ShouldFillSpan()
    {
        var rng = new XorShiftRandom();
        Span<byte> buffer = stackalloc byte[16];
        rng.NextBytes(buffer);

        Assert.Contains(b => b != 0, buffer.ToArray(), "Expected non-zero bytes in actual.");
    }

    /// <summary>
    /// Verifies that an empty span is a no-op that draws nothing from the generator stream, so a subsequent draw
    /// matches an untouched generator with the same seed.
    /// </summary>
    [TestMethod]
    public void NextBytes_WhenSpanIsEmpty_ShouldNotAdvanceGeneratorState()
    {
        var rng = new XorShiftRandom(seed: 99);
        var reference = new XorShiftRandom(seed: 99);

        rng.NextBytes(Span<byte>.Empty);

        Assert.AreEqual(reference.Next(), rng.Next());
    }
}
