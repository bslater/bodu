// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecursiveSelectControl.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

/// <summary>
/// Represents predefined behaviors for how an element is processed during recursive selection.
/// </summary>
/// <remarks>
/// This enum defines composable control flags that determine whether an element should be yielded, whether its children should be
/// traversed, and whether recursion should stop after the current element. These values are intended to be treated as bitmasks using
/// <see cref="int"/> values (not [Flags]).
/// <para>
/// Typical usage includes returning one of these values from a <c>Func&lt;T, RecursiveSelectControl&gt;</c> to control behavior
/// dynamically within a recursive traversal method.
/// </para>
/// <para><strong>Behavior Matrix</strong></para>
/// <list type="table">
/// <listheader>
/// <term>Combination</term>
/// <description>Yield</description>
/// <description>Recurse</description>
/// <description>Break Level</description>
/// <description>Exit All</description>
/// </listheader>
/// <item>
/// <term><see cref="SkipOnly"/></term>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldOnly"/></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndRecurse"/></term>
/// <description>Yes</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndBreak"/></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndExit"/></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// <description>Yes</description>
/// </item>
/// <item>
/// <term><see cref="RecurseOnly"/></term>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndRecurse"/></term>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndBreak"/></term>
/// <description>No</description>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndExit"/></term>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// <description>Yes</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// Common input structure
/// var root = new[]
/// {
///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
///     new Node { Name = "D" }
/// };
/// //
/// RecursiveSelect using different control values
///
/// YieldAndRecurse
/// var all = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndRecurse);
/// Output: A, B, C, D
///
/// SkipOnly
/// var skip = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipOnly);
/// Output: (empty)
///
/// YieldOnly
/// var yieldOnly = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldOnly);
/// Output: A, D
///
/// RecurseOnly
/// var recurseOnly = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.RecurseOnly);
/// Output: B, C
///
/// SkipAndRecurse
/// var skipAndRecurse = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndRecurse);
/// Output: B, C
///
/// YieldAndBreak
/// var yieldAndBreak = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndBreak);
/// Output: A
///
/// SkipAndBreak
/// var skipAndBreak = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndBreak);
/// Output: (empty)
///
/// YieldAndExit
/// var yieldAndExit = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndExit);
/// Output: A
///
/// SkipAndExit
/// var skipAndExit = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndExit);
/// Output: (empty)
///]]>
/// </code>
/// </example>
public enum RecursiveSelectControl
{
    /// <summary>
    /// Yield the current element only, without recursing into children.
    /// </summary>
    YieldOnly = Generic.Extensions.IEnumerableExtensions.Yield,

    /// <summary>
    /// Recurse into children without yielding the current element.
    /// </summary>
    RecurseOnly = Generic.Extensions.IEnumerableExtensions.Recurse,

    /// <summary>
    /// Yield the current element and recurse into its children. This is the default behavior.
    /// </summary>
    YieldAndRecurse = Generic.Extensions.IEnumerableExtensions.Yield | Generic.Extensions.IEnumerableExtensions.Recurse,

    /// <summary>
    /// Skip yielding the current element and do not recurse.
    /// </summary>
    SkipOnly = Generic.Extensions.IEnumerableExtensions.Skip,

    /// <summary>
    /// Skip yielding the current element, but still recurse into its children.
    /// </summary>
    SkipAndRecurse = Generic.Extensions.IEnumerableExtensions.Skip | Generic.Extensions.IEnumerableExtensions.Recurse,

    /// <summary>
    /// Yield the current element, then stop traversal at the current level (like a break).
    /// </summary>
    YieldAndBreak = Generic.Extensions.IEnumerableExtensions.Yield | Generic.Extensions.IEnumerableExtensions.Break,

    /// <summary>
    /// Skip yielding the current element and stop traversal at the current level.
    /// </summary>
    SkipAndBreak = Generic.Extensions.IEnumerableExtensions.Skip | Generic.Extensions.IEnumerableExtensions.Break,

    /// <summary>
    /// Yield the current element, then stop traversal completely across all levels.
    /// </summary>
    YieldAndExit = Generic.Extensions.IEnumerableExtensions.Yield | Generic.Extensions.IEnumerableExtensions.Exit,

    /// <summary>
    /// Skip yielding the current element and stop traversal completely across all levels.
    /// </summary>
    SkipAndExit = Generic.Extensions.IEnumerableExtensions.Skip | Generic.Extensions.IEnumerableExtensions.Exit,
}
