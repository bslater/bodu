// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextSingle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{
    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextSingle()" /> always returns a value in the contractual range
    /// <c>[0.0f, 1.0f)</c>.
    /// </summary>
    [TestMethod]
    public void NextSingle_WhenCalledRepeatedly_ShouldReturnValueInUnitInterval()
    {
        var rng = new XorShiftRandom(seed: 4242);

        for (int i = 0; i < 10_000; i++)
        {
            float value = rng.NextSingle();

            Assert.IsGreaterThanOrEqualTo(0.0f, value);
            Assert.IsLessThan(1.0f, value);
        }
    }

    /// <summary>
    /// Verifies that two generators created with the same seed produce identical
    /// <see cref="XorShiftRandom.NextSingle()" /> sequences, confirming the override draws deterministically from the
    /// documented generator stream.
    /// </summary>
    [TestMethod]
    public void NextSingle_WhenSameSeed_ShouldProduceIdenticalSequence()
    {
        var first = new XorShiftRandom(seed: 777);
        var second = new XorShiftRandom(seed: 777);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(first.NextSingle(), second.NextSingle());
    }
}
