// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RecursiveSelect.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Recursively flattens a hierarchical or tree-like <see cref="IEnumerable" /> into a single linear sequence.
    /// </summary>
    /// <param name="source">
    /// The root sequence to traverse recursively. Each element is expected to potentially contain child elements
    /// retrievable via the <paramref name="childSelector" /> delegate.
    /// </param>
    /// <param name="childSelector">
    /// A delegate that returns the child elements for a given item. This should return an <see cref="IEnumerable" />
    /// representing the recursive children of the current element, or <see langword="null" /> if none.
    /// </param>
    /// <returns>
    /// A flattened <see cref="IEnumerable" /> that yields all elements in depth-first order, including their recursive
    /// children.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source" /> or <paramref name="childSelector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is useful for navigating recursive structures like directory trees, category hierarchies, or comment
    /// threads.
    /// </para>
    /// <para>
    /// Execution is deferred and will only begin when the resulting sequence is enumerated.
    /// </para>
    /// <para>
    /// All elements are treated as <see cref="object" /> and may need casting to their actual types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var root = new[]
    /// {
    ///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
    ///     new Node { Name = "D" }
    /// };
    ///
    /// var flattened = root.RecursiveSelect(n => ((Node)n).Children);
    /// Yields: A, B, C, D
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable RecursiveSelect(this IEnumerable source, Func<object, IEnumerable> childSelector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);

        return Generic.Extensions.IEnumerableExtensions.RecursiveSelectInternal(
            source.Cast<object>(),
            item => (childSelector(item) ?? Array.Empty<object>()).Cast<object>(),
            (item, _, __) => item,
            _ => RecursiveSelectControl.YieldAndRecurse,
            0);
    }

    /// <summary>
    /// Recursively flattens and projects a hierarchical structure into a linear sequence using a projection selector.
    /// </summary>
    /// <param name="source">The root sequence to traverse recursively.</param>
    /// <param name="childSelector">A delegate that returns child elements for a given item.</param>
    /// <param name="selector">A projection applied to each element.</param>
    /// <returns>A depth-first, recursively flattened sequence of projected elements.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or <paramref name="selector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This overload is useful when you want to flatten and transform the hierarchy into a different shape or type.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var root = new[]
    /// {
    ///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
    ///     new Node { Name = "D" }
    /// };
    ///
    /// var names = root.RecursiveSelect(n => ((Node)n).Children, n => ((Node)n).Name);
    /// Yields: "A", "B", "C", "D"
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable RecursiveSelect(
        this IEnumerable source,
        Func<object, IEnumerable<object>> childSelector,
        Func<object, object> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return Generic.Extensions.IEnumerableExtensions.RecursiveSelectInternal(
            source.Cast<object>(),
            childSelector,
            (element, _, __) => selector(element),
            _ => RecursiveSelectControl.YieldAndRecurse,
            0);
    }

    /// <summary>
    /// Recursively flattens and projects a hierarchical structure, passing the index of each element to the projection.
    /// </summary>
    /// <param name="source">The root sequence to traverse recursively.</param>
    /// <param name="childSelector">A delegate that returns child elements for a given item.</param>
    /// <param name="selector">A projection that receives each element and its zero-based index.</param>
    /// <returns>
    /// A depth-first, recursively flattened sequence with projected values using the element and its index.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or <paramref name="selector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Use this when you need both element content and positional context during recursion.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var root = new[]
    /// {
    ///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
    ///     new Node { Name = "D" }
    /// };
    ///
    /// var labeled = root.RecursiveSelect(n => ((Node)n).Children, (n, i) => $"{i}: {((Node)n).Name}");
    /// Yields: "0: A", "1: B", "2: C", "3: D"
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable RecursiveSelect(
        this IEnumerable source,
        Func<object, IEnumerable<object>> childSelector,
        Func<object, int, object> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return Generic.Extensions.IEnumerableExtensions.RecursiveSelectInternal(
            source.Cast<object>(),
            childSelector,
            (element, index, _) => selector(element, index),
            _ => RecursiveSelectControl.YieldAndRecurse,
            0);
    }

    /// <summary>
    /// Recursively flattens and projects a hierarchical structure, providing index and depth information to the
    /// selector.
    /// </summary>
    /// <param name="source">The root sequence to traverse recursively.</param>
    /// <param name="childSelector">A delegate that returns child elements for a given item.</param>
    /// <param name="selector">A projection that receives the element, its index, and its recursion depth.</param>
    /// <returns>A flattened sequence of projected elements with access to structural context (index, depth).</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or <paramref name="selector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Use when formatting output that depends on the depth of the node in the hierarchy (e.g., indentation, styling).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var root = new[]
    /// {
    ///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
    ///     new Node { Name = "D" }
    /// };
    ///
    /// var structured = root.RecursiveSelect(n => ((Node)n).Children,
    ///     (n, i, depth) => new { ((Node)n).Name, Index = i, Depth = depth });
    ///
    /// Yields:
    /// { Name = "A", Index = 0, Depth = 0 }
    /// { Name = "B", Index = 1, Depth = 1 }
    /// { Name = "C", Index = 2, Depth = 1 }
    /// { Name = "D", Index = 3, Depth = 0 }
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable RecursiveSelect(
        this IEnumerable source,
        Func<object, IEnumerable<object>> childSelector,
        Func<object, int, int, object> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return Generic.Extensions.IEnumerableExtensions.RecursiveSelectInternal(
            source.Cast<object>(), childSelector, selector, _ => RecursiveSelectControl.YieldAndRecurse, 0);
    }

    /// <summary>
    /// Recursively flattens and projects a hierarchical structure, allowing control over whether to recurse into
    /// children.
    /// </summary>
    /// <param name="source">The root sequence to traverse recursively.</param>
    /// <param name="childSelector">A delegate that returns child elements for a given item.</param>
    /// <param name="selector">A projection that receives the element, its index, and its recursion depth.</param>
    /// <param name="recursionControl">
    /// A delegate that determines how each element in the sequence should be handled during recursion. It returns a
    /// <see cref="RecursiveSelectControl" /> value indicating whether to yield the element, recurse into its children,
    /// skip it, or terminate the traversal entirely.
    /// </param>
    /// <returns>
    /// A sequence of projected elements, where children are included only if <paramref name="recursionControl" />
    /// returns <see langword="true" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, <paramref name="selector" />, or
    /// <paramref name="recursionControl" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Useful when pruning the recursion tree - e.g., limiting depth, skipping inactive branches, or filtering by
    /// condition.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var root = new[]
    /// {
    ///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
    ///     new Node { Name = "D" }
    /// };
    ///
    /// var pruned = root.RecursiveSelect(
    ///     n => ((Node)n).Children,
    ///     (n, i, d) => ((Node)n).Name,
    ///     n => ((Node)n).Name == "A"
    ///         ? RecursiveSelectControl.YieldAndRecurse
    ///         : RecursiveSelectControl.YieldOnly);
    ///
    /// Yields: "A", "B", "C", "D"
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable RecursiveSelect(
        this IEnumerable source,
        Func<object, IEnumerable<object>> childSelector,
        Func<object, int, int, object> selector,
        Func<object, RecursiveSelectControl> recursionControl)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);
        ThrowHelper.ThrowIfNull(recursionControl);

        return Generic.Extensions.IEnumerableExtensions.RecursiveSelectInternal(
            source.Cast<object>(), childSelector, selector, recursionControl, 0);
    }
}
