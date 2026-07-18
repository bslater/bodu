// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu;

/// <summary>
/// Provides centralized guard clause methods for argument validation, offering consistent and concise exception
/// throwing across the library.
/// </summary>
/// <remarks>
/// <para>
/// This class is split into per-group partial files following the pattern
/// <c>ThrowHelper.&lt;Group&gt;-CallerExpression.cs</c> (for example <c>ThrowHelper.Array-CallerExpression.cs</c>).
/// Each guard relies on <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" /> so the
/// compiler automatically captures argument expressions at the call site.
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
public static partial class ThrowHelper
{
}
