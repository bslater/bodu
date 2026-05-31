// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
/// This class is split across two target-framework-specific partial files to accommodate differences in compiler
/// support for <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>ThrowHelper_NetStandard.cs</c> — compiled when targeting <c>netstandard2.0</c> or <c>netstandard2.1</c>, where
/// <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" /> is unavailable. Parameter names are
/// resolved at the declaration site using <see langword="nameof" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>ThrowHelper_CallerExpression.cs</c> — compiled when targeting modern .NET runtimes that support
/// <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" />, allowing the compiler to
/// automatically capture argument expressions at the call site.
/// </description>
/// </item>
/// </list>
/// <para>
/// Both files expose an identical public API surface. Consumers use the same method signatures regardless of the target
/// framework; the appropriate implementation is selected at compile time.
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
