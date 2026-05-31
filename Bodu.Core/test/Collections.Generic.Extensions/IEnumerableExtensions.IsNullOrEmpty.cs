// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.IsNullOrEmpty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_IsNullOrEmpty
{
    /// <summary>
    /// Verifies that <c>IsNullOrEmpty</c> returns <see langword="true" /> for a <see langword="null" /> sequence.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void IsNullOrEmpty_WhenSourceIsNull_ShouldReturnTrue()
    {
        IEnumerable<int>? source = null;

        Assert.IsTrue(source.IsNullOrEmpty());
    }

    /// <summary>
    /// Verifies that <c>IsNullOrEmpty</c> returns <see langword="true" /> for an empty counted collection.
    /// </summary>
    [TestMethod]
    public void IsNullOrEmpty_WhenSourceIsEmptyCollection_ShouldReturnTrue()
    {
        Assert.IsTrue(Array.Empty<int>().IsNullOrEmpty());
        Assert.IsTrue(new List<int>().IsNullOrEmpty());
    }

    /// <summary>
    /// Verifies that <c>IsNullOrEmpty</c> returns <see langword="false" /> for a non-empty counted collection.
    /// </summary>
    [TestMethod]
    public void IsNullOrEmpty_WhenSourceIsNonEmptyCollection_ShouldReturnFalse() => Assert.IsFalse(new[] { 1 }.IsNullOrEmpty());

    /// <summary>
    /// Verifies that <c>IsNullOrEmpty</c> returns <see langword="true" /> for an empty lazy sequence that exposes no
    /// count.
    /// </summary>
    [TestMethod]
    public void IsNullOrEmpty_WhenSourceIsEmptyLazySequence_ShouldReturnTrue() => Assert.IsTrue(Enumerable.Empty<int>().Where(_ => true).IsNullOrEmpty());

    /// <summary>
    /// Verifies that <c>IsNullOrEmpty</c> returns <see langword="false" /> for a non-empty lazy sequence that exposes
    /// no count.
    /// </summary>
    [TestMethod]
    public void IsNullOrEmpty_WhenSourceIsNonEmptyLazySequence_ShouldReturnFalse() => Assert.IsFalse(Enumerable.Range(1, 3).Where(_ => true).IsNullOrEmpty());
}
