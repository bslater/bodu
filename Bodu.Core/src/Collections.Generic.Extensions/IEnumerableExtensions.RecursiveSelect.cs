// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RecursiveSelect.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Bodu.Collections.Extensions;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Control flag that stops iteration of remaining siblings at the current recursion level.
    /// </summary>
    internal const int Break = 1 << 3;

    /// <summary>
    /// Control flag that terminates the entire recursive traversal immediately.
    /// </summary>
    internal const int Exit = 1 << 4;

    /// <summary>
    /// Control flag that indicates child elements should be processed recursively.
    /// </summary>
    internal const int Recurse = 1 << 1;

    /// <summary>
    /// Control flag that skips yielding the current element in the output sequence.
    /// </summary>
    internal const int Skip = 1 << 2;

    /// <summary>
    /// Control flag that includes the current element in the output sequence.
    /// </summary>
    internal const int Yield = 1 << 0;

    /// <summary>
    /// Recursively flattens a hierarchical sequence using the provided child selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The root sequence to begin recursion from.</param>
    /// <param name="childSelector">A function that returns child elements for a given element.</param>
    /// <returns>A flattened sequence of all elements including their children.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="source" /> or <paramref name="childSelector" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var allNodes = rootNodes.RecursiveSelect(node => node.Children);
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TSource> RecursiveSelect<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);

        return RecursiveSelectInternal(source, childSelector, (element, index, depth) => element, e => RecursiveSelectControl.YieldAndRecurse, 0);
    }

    /// <summary>
    /// Recursively flattens and transforms a hierarchical sequence using the provided child
    /// selector and projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected result elements.</typeparam>
    /// <param name="source">The root sequence to begin recursion from.</param>
    /// <param name="childSelector">A function that returns child elements for a given element.</param>
    /// <param name="selector">A transform function applied to each element.</param>
    /// <returns>
    /// A flattened and projected sequence of results from all elements including children.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or
    /// <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var names = rootNodes.RecursiveSelect(
    ///     node => node.Children,
    ///     node => node.Name);
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> RecursiveSelect<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector,
        Func<TSource, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return RecursiveSelectInternal(source, childSelector, (element, index, depth) => selector(element), e => RecursiveSelectControl.YieldAndRecurse, 0);
    }

    /// <summary>
    /// Recursively flattens and transforms a hierarchical sequence with index using the
    /// provided child selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="source">The root sequence to begin recursion from.</param>
    /// <param name="childSelector">A function that returns child elements for a given element.</param>
    /// <param name="selector">A function applied to each element with its index.</param>
    /// <returns>
    /// A flattened and projected sequence of results from all elements including children.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or
    /// <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var indexedNames = rootNodes.RecursiveSelect(
    ///     node => node.Children,
    ///     (node, index) => $"{index}: {node.Name}");
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> RecursiveSelect<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector,
        Func<TSource, int, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return RecursiveSelectInternal(source, childSelector, (element, index, depth) => selector(element, index), e => RecursiveSelectControl.YieldAndRecurse, 0);
    }

    /// <summary>
    /// Recursively flattens and transforms a hierarchical sequence using child selector and a
    /// selector that receives index and depth.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="source">The root sequence to begin recursion from.</param>
    /// <param name="childSelector">A function that returns child elements for a given element.</param>
    /// <param name="selector">A transform applied to each element, receiving index and depth.</param>
    /// <returns>A flattened and projected sequence from all elements including children.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />, or
    /// <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var formatted = rootNodes.RecursiveSelect(
    ///     node => node.Children,
    ///     (node, index, depth) => new { node.Name, index, depth });
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> RecursiveSelect<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector,
        Func<TSource, int, int, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);

        return RecursiveSelectInternal(source, childSelector, selector, e => RecursiveSelectControl.YieldAndRecurse, 0);
    }

    /// <summary>
    /// Recursively flattens and transforms a hierarchical sequence with depth/index tracking and a delegate that returns a
    /// <see cref="RecursiveSelectControl" /> value to control yielding, recursion, and termination at each node.
    /// </summary>
    /// <typeparam name="TSource">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <param name="source">The root sequence to begin recursion from.</param>
    /// <param name="childSelector">A function that returns child elements for a given element.</param>
    /// <param name="selector">
    /// A transform function applied to each element with index and depth.
    /// </param>
    /// <param name="recursionControl">
    /// A delegate that returns a <see cref="RecursiveSelectControl" /> value for each element, indicating whether to yield it, recurse
    /// into its children, stop processing sibling elements at the current level, or terminate traversal entirely.
    /// </param>
    /// <returns>
    /// A flattened and projected sequence of results. Each element is yielded, skipped, recursed into, or causes traversal to stop
    /// according to the <see cref="RecursiveSelectControl" /> value returned by <paramref name="recursionControl" />.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="source" />, <paramref name="childSelector" />,
    /// <paramref name="selector" />, or <paramref name="recursionControl" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var filtered = rootNodes.RecursiveSelect(
    ///     node => node.Children,
    ///     (node, index, depth) => node.Name,
    ///     node => node.Children.Count > 0
    ///         ? RecursiveSelectControl.YieldAndRecurse
    ///         : RecursiveSelectControl.YieldOnly);
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> RecursiveSelect<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector,
        Func<TSource, int, int, TResult> selector,
        Func<TSource, RecursiveSelectControl> recursionControl)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(childSelector);
        ThrowHelper.ThrowIfNull(selector);
        ThrowHelper.ThrowIfNull(recursionControl);

        return RecursiveSelectInternal(source, childSelector, selector, recursionControl, 0);
    }

    /// <summary>
    /// Recursively traverses a tree-like structure, yielding transformed elements with access to index and depth, and controlling behaviour
    /// at each node using a <see cref="RecursiveSelectControl" /> value.
    /// </summary>
    /// <typeparam name="TSource">The type of input elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of elements yielded by the selector.</typeparam>
    /// <param name="source">The current level of source elements to process.</param>
    /// <param name="childSelector">
    /// A delegate that retrieves the child elements for a given node. May return <c>null</c> or an empty sequence if the node has no children.
    /// </param>
    /// <param name="selector">
    /// A transformation function that projects each yielded element to a result value, and receives the current index (among siblings) and
    /// the recursion depth (starting at 0).
    /// </param>
    /// <param name="recursionControl">
    /// A delegate that returns a <see cref="RecursiveSelectControl" /> value for the given element, indicating whether to yield it, recurse
    /// into its children, or halt traversal.
    /// </param>
    /// <param name="depth">The current recursion depth (zero for top-level elements).</param>
    /// <param name="state">
    /// A shared <see cref="RecursionState" /> object used to track whether a global exit has been requested. This enables short-circuiting
    /// traversal across recursive calls when <c>Exit</c> is encountered.
    /// </param>
    /// <returns>An <see cref="IEnumerable{T}" /> of transformed elements, yielded according to the specified selector and control logic.</returns>
    /// <remarks>
    /// <para>
    /// This method is designed for internal use and does not perform parameter validation. It assumes all inputs are non-null and
    /// consistent with expected contracts. Callers must ensure correct usage.
    /// </para>
    /// <para>The traversal uses a depth-first strategy. At each level:
    /// <list type="bullet">
    /// <item>
    /// <description>If <c>Yield</c> is set and <c>Skip</c> is not set, the element is yielded.</description>
    /// </item>
    /// <item>
    /// <description>If <c>Recurse</c> is set, child elements are visited recursively.</description>
    /// </item>
    /// <item>
    /// <description>If <c>Break</c> is set, remaining siblings are skipped at the current level.</description>
    /// </item>
    /// <item>
    /// <description>If <c>Exit</c> is set, traversal halts immediately across all levels.</description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    internal static IEnumerable<TResult> RecursiveSelectInternal<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TSource>> childSelector,
        Func<TSource, int, int, TResult> selector,
        Func<TSource, RecursiveSelectControl> recursionControl,
        int depth,
        RecursionState? state = null)
    {
        state ??= new RecursionState();
        int index = 0;

        foreach (TSource? element in source)
        {
            if (state.ExitRequested)
                yield break;

            RecursiveSelectControl control = recursionControl(element);

            // Decode the composite control value into its constituent bit flags.
            int flags = (int)control;

            bool shouldYield = (flags & IEnumerableExtensions.Yield) != 0 &&
                               (flags & IEnumerableExtensions.Skip) == 0;

            if (shouldYield)
                yield return selector(element, index++, depth);
            else
                index++;

            // Stop full traversal across all recursion levels.
            if ((flags & IEnumerableExtensions.Exit) != 0)
            {
                state.ExitRequested = true;
                yield break;
            }

            // Stop processing remaining siblings at the current level only.
            if ((flags & IEnumerableExtensions.Break) != 0)
                break;

            // Recurse into children if requested and available.
            if ((flags & IEnumerableExtensions.Recurse) != 0)
            {
                IEnumerable<TSource>? children = childSelector(element);
                if (children is null)
                    continue;

                foreach (TResult? child in RecursiveSelectInternal(children, childSelector, selector, recursionControl, depth + 1, state))
                {
                    if (state.ExitRequested)
                        yield break;

                    yield return child;
                }
            }
        }
    }

    /// <summary>
    /// Represents shared traversal state for recursive selection routines, coordinating early-exit behaviour across all recursion levels.
    /// </summary>
    internal sealed class RecursionState
    {
        /// <summary>
        /// Gets or sets a value indicating whether full traversal has been terminated.
        /// </summary>
        public bool ExitRequested { get; set; }
    }
}
