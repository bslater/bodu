// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.Properties.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BernsteinTests
{
    /// <summary>
    /// Verifies that the parameterless constructor selects the original (non-modified) djb2 form, and that
    /// reading the <see cref="Bernstein.UseModifiedAlgorithm" /> getter returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenDefaultConstructed_ShouldReturnFalse()
    {
        Bernstein algorithm = new();
        Assert.IsFalse(algorithm.UseModifiedAlgorithm);
    }

    /// <summary>
    /// Verifies that constructing with <c>useModifiedAlgorithm: true</c> causes
    /// <see cref="Bernstein.UseModifiedAlgorithm" /> to return <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenConstructedWithModifiedFlag_ShouldReturnTrue()
    {
        Bernstein algorithm = new(Bernstein.DefaultInitialValue, useModifiedAlgorithm: true);
        Assert.IsTrue(algorithm.UseModifiedAlgorithm);
    }

    /// <summary>
    /// Verifies that setting <see cref="Bernstein.UseModifiedAlgorithm" /> through the setter is observable
    /// through the getter on the same instance.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSet_ShouldBeObservableThroughGetter()
    {
        Bernstein algorithm = new() { UseModifiedAlgorithm = true };
        Assert.IsTrue(algorithm.UseModifiedAlgorithm);

        algorithm.Reset();
        algorithm.UseModifiedAlgorithm = false;
        Assert.IsFalse(algorithm.UseModifiedAlgorithm);
    }
}
