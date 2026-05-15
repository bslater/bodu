// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Formats;

/// <summary>
/// Provides centralized guard clause and terminal-throw helpers for the Bencode format implementation. Pairs with
/// the catalogue in <see cref="Bodu.ThrowHelper" /> by re-exporting the few generic guards that the format actually
/// uses and adding format-specific helpers backed by <see cref="FormatsResourceStrings" />.
/// </summary>
/// <remarks>
/// <para>
/// The class follows the same partial-file pattern used by <see cref="Bodu.ThrowHelper" /> in
/// <c>Bodu.Core</c>:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>ThrowHelper.cs</c> — root declaration with assembly-level suppression attributes covering the compact
/// guard/throw clauses used throughout the helpers.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>ThrowHelper.CallerExpression.cs</c> — implementation file containing the cached
/// <see cref="System.Text.CompositeFormat" /> state, the forwarding wrapper for
/// <see cref="Bodu.ThrowHelper.ThrowIfNull{T}(T, string)" />, and every format-specific terminal throw.
/// </description>
/// </item>
/// </list>
/// <para>
/// The class is <c>internal</c> because the helpers are validation primitives consumed only by the project's
/// own production code and its <c>InternalsVisibleTo</c>-paired test assembly.
/// </para>
/// </remarks>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "ThrowHelper methods intentionally use compact guard/throw clauses; adding braces adds noise without improving control-flow clarity.")]

[SuppressMessage(
    "Roslynator",
    "RCS1001:Add braces (when expression spans over multiple lines)",
    Justification = "ThrowHelper methods intentionally use compact guard/throw clauses; adding braces adds noise without improving control-flow clarity.")]
internal static partial class ThrowHelper
{
}
