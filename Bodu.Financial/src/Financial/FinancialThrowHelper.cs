// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelper.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial;

/// <summary>
/// Provides <c>ThrowIf</c> guard helpers shared by multiple monetary and exchange-rate primitives in
/// <c>Bodu.Financial</c>.
/// </summary>
/// <remarks>
/// <para>
/// Single-use guards are kept at their call sites or expressed as <c>private static</c> helpers on the owning type.
/// Only guards reused across two or more classes — and that test a condition rather than unconditionally throwing —
/// belong here.
/// </para>
/// <para>
/// The class follows the same partial-file pattern used by <see cref="Bodu.ThrowHelper" /> in <c>Bodu.Core</c> and
/// <c>CalendarThrowHelper</c> in <c>Bodu.Globalization.Calendar</c>: this root holds the declaration while
/// <c>FinancialThrowHelper.CallerExpression.cs</c> holds the implementation.
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
internal static partial class FinancialThrowHelper
{
}
