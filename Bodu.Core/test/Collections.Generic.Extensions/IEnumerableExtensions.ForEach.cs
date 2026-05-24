// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ForEach.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_ForEach
{
    /// <summary>
    /// Verifies that <c>ForEach</c> invokes the action once for each element, in sequence order.
    /// </summary>
    [TestMethod]
    public void ForEach_WhenInvoked_ShouldCallActionForEachElementInOrder()
    {
        List<int> observed = new();

        new[] { 1, 2, 3 }.ForEach(observed.Add);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, observed);
    }

    /// <summary>
    /// Verifies that <c>ForEach</c> does not invoke the action for an empty sequence.
    /// </summary>
    [TestMethod]
    public void ForEach_WhenSourceIsEmpty_ShouldNotInvokeAction()
    {
        int count = 0;

        Array.Empty<int>().ForEach(_ => count++);

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that <c>ForEach</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ForEach_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            source.ForEach(_ => { });
        });
    }

    /// <summary>
    /// Verifies that <c>ForEach</c> throws <see cref="ArgumentNullException" /> when the action is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ForEach_WhenActionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            new[] { 1, 2, 3 }.ForEach(null!);
        });
    }
}
