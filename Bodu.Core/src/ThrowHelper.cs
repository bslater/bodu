// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu;

/// <summary>
/// Provides centralized guard clause methods for argument validation, offering consistent and concise exception
/// throwing across both legacy and modern .NET target frameworks.
/// </summary>
/// <remarks>
/// <para>
/// This class is split into per-group partial files following the pattern
/// <c>ThrowHelper.&lt;Group&gt;-{CallerExpression,NetStandard}.cs</c> (for example
/// <c>ThrowHelper.Array-CallerExpression.cs</c> / <c>ThrowHelper.Array-NetStandard.cs</c>), to accommodate differences
/// in compiler support for <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>ThrowHelper.&lt;Group&gt;-CallerExpression.cs</c> — compiled for modern .NET runtimes (guarded by
/// <c>#if !NETSTANDARD2_0_OR_GREATER</c>), where the compiler automatically captures argument expressions at the call
/// site.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>ThrowHelper.&lt;Group&gt;-NetStandard.cs</c> — guarded by <c>#if NETSTANDARD2_0_OR_GREATER</c> for hypothetical
/// <c>netstandard2.x</c> targets, where the attribute is unavailable and parameter names are supplied with
/// <see langword="nameof" /> at the call site.
/// </description>
/// </item>
/// </list>
/// <para>
/// The project currently targets <c>net8.0</c> only, so the <c>NetStandard</c> partials are inactive and their surface
/// has drifted from the <c>CallerExpression</c> partials — they are retained as a starting point should a
/// <c>netstandard</c> target return, not as a guaranteed mirror of the active API.
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
