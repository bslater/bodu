// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextThrowHelper.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text;

/// <summary>
/// Provides <c>ThrowIf</c> guard helpers for stream capability checks that are not covered by
/// <see cref="Bodu.ThrowHelper" />.
/// </summary>
/// <remarks>
/// <para>
/// All format-specific, single-use exceptions are thrown inline at their call sites. Only guards that are reused across
/// multiple call sites and that test a condition (rather than unconditionally throwing) belong here.
/// </para>
/// <para>
/// The class follows the same partial-file pattern used by <see cref="Bodu.ThrowHelper" /> in <c>Bodu.Core</c>:
/// <c>TextThrowHelper.cs</c> holds the root declaration and <c>TextThrowHelper.CallerExpression.cs</c> holds the
/// implementations, which use <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" /> to
/// capture the parameter name automatically.
/// </para>
/// <para>
/// The class is <c>internal</c> because the helpers are validation primitives consumed only by the project's own
/// production code and its <c>InternalsVisibleTo</c>-paired test assembly.
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
internal static partial class TextThrowHelper
{
}
