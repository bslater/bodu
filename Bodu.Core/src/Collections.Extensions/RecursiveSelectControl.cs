// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecursiveSelectControl.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

/// <summary>
/// Represents composable control flags for how an element is processed during recursive selection.
/// </summary>
/// <remarks>
/// This enum defines composable control flags that determine whether an element should be yielded, whether its children
/// should be traversed, and whether traversal should stop after the current element. It is a genuine
/// <see cref="FlagsAttribute" /> bitmask: the primitive flags <see cref="Yield" />, <see cref="Recurse" />,
/// <see cref="Skip" />, <see cref="Break" />, and <see cref="Exit" /> may be combined with the bitwise-OR operator, and
/// the named combinations below are provided for the common cases.
/// <para>
/// <see cref="Break" /> and <see cref="Recurse" /> are orthogonal and compose: <see cref="Break" /> stops iteration of
/// the remaining siblings at the current level, whereas <see cref="Recurse" /> requests descent into the current
/// element's children. When both are set, the current element's children are visited first and only then are the
/// remaining siblings skipped.
/// </para>
/// <para>
/// Typical usage includes returning one of these values from a <c>Func&lt;T, RecursiveSelectControl&gt;</c> to control
/// behavior dynamically within a recursive traversal method.
/// </para>
/// <para>
/// <strong>Behavior Matrix</strong>
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Combination</term>
/// <description>Yield</description>
/// <description>Recurse</description>
/// <description>Break Level</description>
/// <description>Exit All</description>
/// </listheader>
/// <item>
/// <term><see cref="SkipOnly" /></term>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldOnly" /></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndRecurse" /></term>
/// <description>Yes</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndBreak" /></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="YieldAndExit" /></term>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// <description>Yes</description>
/// </item>
/// <item>
/// <term><see cref="RecurseOnly" /></term>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndRecurse" /></term>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndBreak" /></term>
/// <description>No</description>
/// <description>No</description>
/// <description>Yes</description>
/// <description>No</description>
/// </item>
/// <item>
/// <term><see cref="SkipAndExit" /></term>
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
/// // Common input structure
/// var root = new[]
/// {
///     new Node { Name = "A", Children = { new Node { Name = "B" }, new Node { Name = "C" } } },
///     new Node { Name = "D" }
/// };
/// //
/// // RecursiveSelect using different control values
///
/// // YieldAndRecurse
/// var all = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndRecurse);
/// // Output: A, B, C, D
///
/// // SkipOnly
/// var skip = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipOnly);
/// // Output: (empty)
///
/// // YieldOnly
/// var yieldOnly = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldOnly);
/// // Output: A, D
///
/// // RecurseOnly
/// var recurseOnly = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.RecurseOnly);
/// // Output: B, C
///
/// // SkipAndRecurse
/// var skipAndRecurse = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndRecurse);
/// // Output: B, C
///
/// // YieldAndBreak
/// var yieldAndBreak = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndBreak);
/// // Output: A
///
/// // SkipAndBreak
/// var skipAndBreak = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndBreak);
/// // Output: (empty)
///
/// // YieldAndExit
/// var yieldAndExit = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.YieldAndExit);
/// // Output: A
///
/// // SkipAndExit
/// var skipAndExit = root.RecursiveSelect(
///     n => n.Children,
///     (n, i, d) => n.Name,
///     n => RecursiveSelectControl.SkipAndExit);
/// // Output: (empty)
///]]>
/// </code>
/// </example>
[Flags]
public enum RecursiveSelectControl
{
    /// <summary>
    /// No flags set: the current element is neither yielded nor recursed into.
    /// </summary>
    None = 0,

    /// <summary>
    /// Primitive flag that includes the current element in the output sequence.
    /// </summary>
    Yield = 1 << 0,

    /// <summary>
    /// Primitive flag that requests recursion into the current element's children.
    /// </summary>
    Recurse = 1 << 1,

    /// <summary>
    /// Primitive flag that skips yielding the current element. Overrides <see cref="Yield" /> when both are set.
    /// </summary>
    Skip = 1 << 2,

    /// <summary>
    /// Primitive flag that stops iteration of the remaining siblings at the current recursion level.
    /// </summary>
    Break = 1 << 3,

    /// <summary>
    /// Primitive flag that terminates the entire recursive traversal immediately.
    /// </summary>
    Exit = 1 << 4,

    /// <summary>
    /// Yield the current element only, without recursing into children.
    /// </summary>
    YieldOnly = Yield,

    /// <summary>
    /// Recurse into children without yielding the current element.
    /// </summary>
    RecurseOnly = Recurse,

    /// <summary>
    /// Yield the current element and recurse into its children. This is the default behavior.
    /// </summary>
    YieldAndRecurse = Yield | Recurse,

    /// <summary>
    /// Skip yielding the current element and do not recurse.
    /// </summary>
    SkipOnly = Skip,

    /// <summary>
    /// Skip yielding the current element, but still recurse into its children.
    /// </summary>
    SkipAndRecurse = Skip | Recurse,

    /// <summary>
    /// Yield the current element, then stop traversal at the current level (like a break).
    /// </summary>
    YieldAndBreak = Yield | Break,

    /// <summary>
    /// Skip yielding the current element and stop traversal at the current level.
    /// </summary>
    SkipAndBreak = Skip | Break,

    /// <summary>
    /// Yield the current element, then stop traversal completely across all levels.
    /// </summary>
    YieldAndExit = Yield | Exit,

    /// <summary>
    /// Skip yielding the current element and stop traversal completely across all levels.
    /// </summary>
    SkipAndExit = Skip | Exit,
}
