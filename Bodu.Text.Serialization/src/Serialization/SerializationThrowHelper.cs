// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides <c>ThrowIf</c> guard helpers shared by the serializer facades that are not covered by
/// <see cref="Bodu.ThrowHelper" />.
/// </summary>
/// <remarks>
/// <para>
/// The class follows the partial-file pattern used by <see cref="Bodu.ThrowHelper" />: this file holds the root
/// declaration and <c>SerializationThrowHelper.CallerExpression.cs</c> holds the implementations, which use
/// <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" /> to capture the parameter name.
/// </para>
/// <para>
/// The guards are <c>internal</c> and consumed by the format serializer assemblies through <c>InternalsVisibleTo</c>.
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
internal static partial class SerializationThrowHelper
{
}
