// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.Next.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />(min, max) returns values within the expected bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0, 10)]
    [DataRow(-10, 0)]
    [DataRow(50, 100)]
    public void Next_ShouldBeInRange_WhenGivenBounds(int min, int max)
    {
        var rng = new XorShiftRandom();
        for (int i = 0; i < 100; i++)
        {
            int value = rng.Next(min, max);
            Assert.IsTrue(value >= min && value < max, $"Value {value} was not in range [{min}, {max}).");
        }
    }

    /// <summary>
    /// Verifies that different seeds produce different sequences using the default Next() method.
    /// </summary>
    [TestMethod]
    [DataRow(123)]
    [DataRow(456)]
    [DataRow(789)]
    public void Next_ShouldReturnDifferentValues_WithDifferentSeeds(int seed)
    {
        var rng1 = new XorShiftRandom(seed);
        var rng2 = new XorShiftRandom(seed + 1);

        int value1 = rng1.Next();
        int value2 = rng2.Next();

        Assert.AreNotEqual(value1, value2, "Different seeds should produce different sequences.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when Called, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Next_WhenCalled_ShouldReturnPositiveInteger()
    {
        var rng = new XorShiftRandom();
        int value = rng.Next();
        Assert.IsGreaterThanOrEqualTo(0, value);
    }

    /// <summary>
    /// Verifies that calling <see cref="XorShiftRandom.Next" /> multiple times produces values in range and not all equal.
    /// </summary>
    [TestMethod]
    public void Next_WhenCalledMultipleTimes_ShouldProduceNonRepeatingValues()
    {
        var rng = new XorShiftRandom();
        var values = new HashSet<int>();

        for (int i = 0; i < 50; i++)
        {
            values.Add(rng.Next(1000));
        }

        Assert.IsGreaterThan(1, values.Count, "Next produced repeating or identical values.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when CalledWithinAndMax, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 10)]
    [DataRow(5, 15)]
    public void Next_WhenCalledWithinAndMax_ShouldRespectBounds(int min, int max)
    {
        var rng = new XorShiftRandom(42);
        for (int i = 0; i < 1000; i++)
        {
            int value = rng.Next(min, max);
            Assert.IsTrue(value >= min && value < max);
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when CalledWithMax, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(10)]
    [DataRow(100)]
    public void Next_WhenCalledWithMax_ShouldReturnWithinRange(int maxValue)
    {
        var rng = new XorShiftRandom(42);
        for (int i = 0; i < 1000; i++)
        {
            int value = rng.Next(maxValue);
            Assert.IsTrue(value >= 0 && value < maxValue);
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="XorShiftRandom.Next" /> with maxValue = 1 always returns 0.
    /// </summary>
    [TestMethod]
    public void Next_WhenMaxValueIsOne_ShouldAlwaysReturnZero()
    {
        var rng = new XorShiftRandom();
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(0, rng.Next(1), "Next(1) should always return 0.");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="XorShiftRandom.Next" /> with a positive maxValue returns a value in the expected range.
    /// </summary>
    [TestMethod]
    public void Next_WhenMaxValueIsPositive_ShouldReturnValueInRange()
    {
        var rng = new XorShiftRandom();
        int actual = rng.Next(100);

        Assert.IsTrue(actual is >= 0 and < 100, $"Result {actual} is out of expected range.");
    }
    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when SeedIsNotSame, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Next_WhenSeedIsNotSame_ShouldProduceDeterministicSequence()
    {
        var rng1 = new XorShiftRandom(42);
        var rng2 = new XorShiftRandom(42);

        int[] sequence1 = Enumerable.Range(0, 10).Select(_ => rng1.Next()).ToArray();
        int[] sequence2 = Enumerable.Range(0, 10).Select(_ => rng2.Next()).ToArray();

        CollectionAssert.AreEqual(sequence1, sequence2);
    }

    /// <summary>
    /// Verifies that two instances with different seeds produce different sequences.
    /// </summary>
    [TestMethod]
    public void Next_WithDifferentSeeds_ShouldProduceDifferentSequences()
    {
        var rng1 = new XorShiftRandom(100);
        var rng2 = new XorShiftRandom(200);

        bool differenceFound = false;
        for (int i = 0; i < 20; i++)
        {
            if (rng1.Next(1000) != rng2.Next(1000))
            {
                differenceFound = true;
                break;
            }
        }

        Assert.IsTrue(differenceFound, "Two RNGs with different seeds produced identical sequences.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, with MaxValue, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Next_WithMaxValue_ShouldReturnInExpectedRange()
    {
        var rng = new XorShiftRandom();
        for (int i = 0; i < 1000; i++)
        {
            int actual = rng.Next(100);
            Assert.IsTrue(actual is >= 0 and < 100);
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, with MaxValueZero, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void Next_WithMaxValueZero_ShouldThrowExactly(int maxValue)
    {
        var rng = new XorShiftRandom();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rng.Next(maxValue));
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, with MinAndMax, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Next_WithMinAndMax_ShouldReturnWithinRange()
    {
        var rng = new XorShiftRandom();
        for (int i = 0; i < 1000; i++)
        {
            int actual = rng.Next(10, 20);
            Assert.IsTrue(actual is >= 10 and < 20);
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next(int, int)" /> throws when
    /// <paramref name="minValue" /> is strictly greater than <paramref name="maxValue" />.
    /// </summary>
    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(10, 5)]
    [DataRow(0, -1)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void Next_WhenMinGreaterThanMax_ShouldThrowExactly(int minValue, int maxValue)
    {
        var rng = new XorShiftRandom();
        Assert.ThrowsExactly<ArgumentException>(() => rng.Next(minValue, maxValue));
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next(int, int)" /> returns
    /// <paramref name="value" /> when invoked with an empty range
    /// (<c>minValue == maxValue</c>), matching the documented behaviour of
    /// <see cref="System.Random.Next(int, int)" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Next_WhenMinEqualsMax_ShouldReturnMinValue(int value)
    {
        var rng = new XorShiftRandom(seed: 1234);
        Assert.AreEqual(value, rng.Next(value, value));
    }

    /// <summary>
    /// Verifies that two instances with the same seed produce the same sequence.
    /// </summary>
    [TestMethod]
    public void Next_WithSameSeed_ShouldProduceSameSequence()
    {
        uint seed = 123456;
        var rng1 = new XorShiftRandom(seed);
        var rng2 = new XorShiftRandom(seed);

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(rng1.Next(1000), rng2.Next(1000), $"Mismatch at iteration {i}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.BoundedNextUInt32(uint, System.Func{uint})" /> rejects a biased
    /// low-bit sample and redraws from the source. Drives the helper with a scripted source so the rejection
    /// path is directly observable: for <c>maxExclusive == 3</c>, the input <c>0u</c> yields <c>lowBits == 0</c>
    /// which is below the rejection threshold of <c>2^32 mod 3 == 1</c> and must be retried.
    /// </summary>
    [TestMethod]
    public void BoundedNextUInt32_WhenLowBitsBelowThreshold_ShouldRejectAndRedraw()
    {
        var queue = new Queue<uint>([0u, 0x55555556u]);

        uint actual = XorShiftRandom.BoundedNextUInt32(3u, () => queue.Dequeue());

        Assert.IsEmpty(queue, "Expected the biased first value to be rejected and the second value consumed.");
        Assert.AreEqual(1u, actual);
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.BoundedNextUInt32(uint, System.Func{uint})" /> returns immediately
    /// when the first draw lies above the rejection threshold, without consuming additional source values.
    /// </summary>
    [TestMethod]
    public void BoundedNextUInt32_WhenLowBitsAboveThreshold_ShouldReturnFromFirstDraw()
    {
        int calls = 0;

        uint actual = XorShiftRandom.BoundedNextUInt32(3u, () =>
        {
            calls++;
            return 0x55555556u;
        });

        Assert.AreEqual(1, calls, "Expected exactly one source call when the first draw is above the rejection threshold.");
        Assert.AreEqual(1u, actual);
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.BoundedNextUInt32(uint, System.Func{uint})" /> always returns a value
    /// strictly less than <paramref name="maxExclusive" /> across the full uint input space, including the high
    /// edge where modulo bias would have produced an out-of-range result with the previous implementation.
    /// </summary>
    [TestMethod]
    [DataRow(2u)]
    [DataRow(3u)]
    [DataRow(7u)]
    [DataRow(1000u)]
    [DataRow((uint)int.MaxValue)]
    public void BoundedNextUInt32_WhenSourceProducesHighValue_ShouldReturnInRange(uint maxExclusive)
    {
        uint[] samples = new[] { uint.MaxValue, uint.MaxValue - 1u, 0u, 1u, 0x80000000u };

        foreach (uint sample in samples)
        {
            var queue = new Queue<uint>();
            queue.Enqueue(sample);
            for (int i = 0; i < 4; i++) queue.Enqueue(0x55555556u); // fallback values for redraws

            uint actual = XorShiftRandom.BoundedNextUInt32(maxExclusive, () => queue.Dequeue());
            Assert.IsLessThan(maxExclusive, actual, $"sample={sample:X}, maxExclusive={maxExclusive}, actual={actual}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next(int, int)" /> handles the full-int range
    /// <c>[int.MinValue, int.MaxValue)</c> without overflow and returns values inside the documented bounds.
    /// </summary>
    [TestMethod]
    public void Next_WhenRangeIsFullInt_ShouldReturnInRangeWithoutOverflow()
    {
        var rng = new XorShiftRandom(42);

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.Next(int.MinValue, int.MaxValue);
            Assert.IsTrue(value is >= int.MinValue and < int.MaxValue, $"value={value} outside [int.MinValue, int.MaxValue).");
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next(int, int)" /> across the full-int range can reach values
    /// in both the lower and upper halves of <see cref="int" />, guarding against any future regression that
    /// collapses the range to a half-interval (for example, by truncating the unsigned range to <c>int.MaxValue</c>).
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Next_WhenRangeIsFullInt_ShouldReachBothHalves()
    {
        var rng = new XorShiftRandom(42);
        bool sawLower = false;
        bool sawUpper = false;

        for (int i = 0; i < 1_000_000 && (!sawLower || !sawUpper); i++)
        {
            int value = rng.Next(int.MinValue, int.MaxValue);
            if (value < int.MinValue / 2) sawLower = true;
            if (value > int.MaxValue / 2) sawUpper = true;
        }

        Assert.IsTrue(sawLower, "Did not observe any value in the lower half of int after 1,000,000 draws.");
        Assert.IsTrue(sawUpper, "Did not observe any value in the upper half of int after 1,000,000 draws.");
    }

}
