// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensionsTests.RecursiveSelect.ChildSelectorVariants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Extensions;

public partial class IEnumerableExtensionsTests_RecursiveSelect
{

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerableExtensions.RecursiveSelect(IEnumerable, Func{object, IEnumerable})" /> overload
    /// substitutes an empty children sequence when the child selector returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RecursiveSelect_NonGeneric_WhenChildSelectorReturnsNull_ShouldSubstituteEmptyChildren()
    {
        IEnumerable source = new object[] { new Node { Name = "Solo" } };

        var actual = source
            .RecursiveSelect((Func<object, IEnumerable>)(_ => null!))
            .Cast<Node>()
            .Select(n => n.Name)
            .ToList();

        CollectionAssert.AreEqual(new[] { "Solo" }, actual);
    }

    /// <summary>
    /// Verifies that the indexed-projection overload of <see cref="IEnumerableExtensions.RecursiveSelect" /> exposes the zero-based index
    /// of each yielded element.
    /// </summary>
    [TestMethod]
    public void RecursiveSelect_NonGeneric_WhenIndexedSelectorIsSupplied_ShouldExposeIndex()
    {
        IEnumerable source = NodeSampleTree.BuildSampleTree();

        var actual = source
            .RecursiveSelect(
                childSelector: e => ((Node)e).Children.Cast<object>(),
                selector: (e, i) => $"{i}:{((Node)e).Name}")
            .Cast<string>()
            .ToArray();

        Assert.AreEqual("0:Root", actual[0]);
        Assert.AreEqual("0:A", actual[1]);
        Assert.AreEqual("1:B", actual[2]);
    }

    /// <summary>
    /// Verifies that the simple-projection overload of <see cref="IEnumerableExtensions.RecursiveSelect" /> applies the supplied selector
    /// to each visited element.
    /// </summary>
    [TestMethod]
    public void RecursiveSelect_NonGeneric_WhenSelectorIsSupplied_ShouldApplyProjection()
    {
        IEnumerable source = NodeSampleTree.BuildSampleTree();

        var actual = source
            .RecursiveSelect(
                childSelector: e => ((Node)e).Children.Cast<object>(),
                selector: e => ((Node)e).Name)
            .Cast<string>()
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Root", "A", "B", "B1", "B2", "C", "C1", "C1A", "C2", "C2A", "C2B", "C2C", "D", "E" },
            actual);
    }

}
