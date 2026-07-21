// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.NextInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{
    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextInt64()" /> always returns a value in the contractual range
    /// <c>[0, long.MaxValue)</c>.
    /// </summary>
    [TestMethod]
    public void NextInt64_WhenCalledRepeatedly_ShouldReturnValueInContractRange()
    {
        var rng = new XorShiftRandom(seed: 4242);

        for (int i = 0; i < 10_000; i++)
        {
            long value = rng.NextInt64();

            Assert.IsGreaterThanOrEqualTo(0L, value);
            Assert.IsLessThan(long.MaxValue, value);
        }
    }

    /// <summary>
    /// Verifies that two generators created with the same seed produce identical <see cref="XorShiftRandom.NextInt64()" />
    /// sequences, confirming the override draws deterministically from the documented generator stream.
    /// </summary>
    [TestMethod]
    public void NextInt64_WhenSameSeed_ShouldProduceIdenticalSequence()
    {
        var first = new XorShiftRandom(seed: 777);
        var second = new XorShiftRandom(seed: 777);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(first.NextInt64(), second.NextInt64());
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.NextInt64()" /> exercises the upper half of the 63-bit range, which the
    /// two-draw composition must be able to reach.
    /// </summary>
    [TestMethod]
    public void NextInt64_WhenCalledRepeatedly_ShouldProduceValuesAboveIntRange()
    {
        var rng = new XorShiftRandom(seed: 1);

        bool sawLarge = false;
        for (int i = 0; i < 1_000 && !sawLarge; i++)
            sawLarge = rng.NextInt64() > int.MaxValue;

        Assert.IsTrue(sawLarge, "Expected at least one draw above int.MaxValue within 1000 attempts.");
    }
}
