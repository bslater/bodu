// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SystemRandomAdapterTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed class SystemRandomAdapterTests
{
    /// <summary>
    /// Verifies that <see cref="SystemRandomAdapter(Random)" /> throws <see cref="ArgumentNullException" /> when the wrapped random
    /// instance is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRandomIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SystemRandomAdapter(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SystemRandomAdapter(Random)" /> wraps the supplied <see cref="Random" /> instance and produces the same
    /// sequence under a seeded random.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRandomIsSeeded_ShouldProduceDeterministicSequence()
    {
        var adapter1 = new SystemRandomAdapter(new Random(42));
        var adapter2 = new SystemRandomAdapter(new Random(42));

        for (int i = 0; i < 5; i++)
            Assert.AreEqual(adapter1.Next(100), adapter2.Next(100));
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="SystemRandomAdapter()" /> constructor produces values within the requested range.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_ShouldProduceValuesWithinRange()
    {
        var adapter = new SystemRandomAdapter();

        for (int i = 0; i < 100; i++)
        {
            int value = adapter.Next(10);
            Assert.IsTrue(value >= 0 && value < 10, $"Value {value} is outside [0, 10).");
        }
    }
}
