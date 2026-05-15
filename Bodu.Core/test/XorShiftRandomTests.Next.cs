// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.Next.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        for (var i = 0; i < 100; i++)
        {
            var value = rng.Next(min, max);
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

        var value1 = rng1.Next();
        var value2 = rng2.Next();

        Assert.AreNotEqual(value1, value2, "Different seeds should produce different sequences.");
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when Called, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Next_WhenCalled_ShouldReturnPositiveInteger()
    {
        var rng = new XorShiftRandom();
        var value = rng.Next();
        Assert.IsTrue(value >= 0);
    }

    /// <summary>
    /// Verifies that calling <see cref="XorShiftRandom.Next" /> multiple times produces values in range and not all equal.
    /// </summary>
    [TestMethod]
    public void Next_WhenCalledMultipleTimes_ShouldProduceNonRepeatingValues()
    {
        var rng = new XorShiftRandom();
        var values = new HashSet<int>();

        for (var i = 0; i < 50; i++)
        {
            values.Add(rng.Next(1000));
        }

        Assert.IsTrue(values.Count > 1, "Next produced repeating or identical values.");
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
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(min, max);
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
        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(maxValue);
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
        for (var i = 0; i < 10; i++)
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
        var actual = rng.Next(100);

        Assert.IsTrue(actual >= 0 && actual < 100, $"Result {actual} is out of expected range.");
    }
    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />, when SeedIsNotSame, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Next_WhenSeedIsNotSame_ShouldProduceDeterministicSequence()
    {
        var rng1 = new XorShiftRandom(42);
        var rng2 = new XorShiftRandom(42);

        var sequence1 = Enumerable.Range(0, 10).Select(_ => rng1.Next()).ToArray();
        var sequence2 = Enumerable.Range(0, 10).Select(_ => rng2.Next()).ToArray();

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

        var differenceFound = false;
        for (var i = 0; i < 20; i++)
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
        for (var i = 0; i < 1000; i++)
        {
            var actual = rng.Next(100);
            Assert.IsTrue(actual >= 0 && actual < 100);
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
        for (var i = 0; i < 1000; i++)
        {
            var actual = rng.Next(10, 20);
            Assert.IsTrue(actual >= 10 && actual < 20);
        }
    }

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Next" />(min, max) throws when min is greater than max.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(1, 1)]
    [DataRow(10, 5)]
    public void Next_WithMinGreaterThanMax_ShouldThrowExactly(int minValue, int maxValue)
    {
        var rng = new XorShiftRandom();
        Assert.ThrowsExactly<ArgumentException>(() => rng.Next(minValue, maxValue));
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

        for (var i = 0; i < 20; i++)
        {
            Assert.AreEqual(rng1.Next(1000), rng2.Next(1000), $"Mismatch at iteration {i}");
        }
    }

}
