// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RecursiveSelect.SelectorOnly.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public partial class IEnumerableExtensionsTests_RecursiveSelect
{

    /// <summary>
    /// Verifies that the projection overload <see cref="IEnumerableExtensions.RecursiveSelect{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TSource}}, Func{TSource, TResult})" />
    /// applies the supplied selector to every visited element in depth-first order.
    /// </summary>
    [TestMethod]
    public void RecursiveSelect_SelectorOnly_ShouldApplyProjectionToEveryElement()
    {
        Node[] root = NodeSampleTree.BuildSampleTree();

        var actual = root.RecursiveSelect(
            childSelector: n => n.Children,
            selector: n => n.Name).ToArray();

        CollectionAssert.AreEqual(
            new[] { "Root", "A", "B", "B1", "B2", "C", "C1", "C1A", "C2", "C2A", "C2B", "C2C", "D", "E" },
            actual);
    }

}
