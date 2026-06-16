// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SystemRandomAdapterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed class SystemRandomAdapterTests
{

    /// <summary>
    /// Verifies that the parameterless <see cref="SystemRandomAdapter()" /> constructor produces values within the requested range.
    /// </summary>
    [TestMethod]
    public void Ctor_Parameterless_ShouldProduceValuesWithinRange()
    {
        var adapter = new SystemRandomAdapter();

        for (int i = 0; i < 100; i++)
        {
            int value = adapter.Next(10);
            Assert.IsTrue(value is >= 0 and < 10, $"Value {value} is outside [0, 10).");
        }
    }
    /// <summary>
    /// Verifies that <see cref="SystemRandomAdapter(Random)" /> throws <see cref="ArgumentNullException" /> when the wrapped random
    /// instance is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRandomIsNull_ShouldThrowExactly()
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
    public void Ctor_WhenRandomIsSeeded_ShouldProduceDeterministicSequence()
    {
        var adapter1 = new SystemRandomAdapter(new Random(42));
        var adapter2 = new SystemRandomAdapter(new Random(42));

        for (int i = 0; i < 5; i++)
            Assert.AreEqual(adapter1.Next(100), adapter2.Next(100));
    }

}
