// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculationAnchorCacheKey.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies a cached calculation anchor resolved for a specific civil year.
/// </summary>
/// <param name="AnchorRuleName">The name of the rule that provides the calculation anchor.</param>
/// <param name="Year">The civil year for which the calculation anchor was resolved.</param>
/// <remarks>
/// <para>
/// Calculation anchors are internal dates used to resolve other notable-date rules. For example, a rule such as
/// <c>Start of Lent</c> may be calculated as an offset from an <c>Easter Sunday</c> anchor.
/// </para>
/// <para>
/// A calculation anchor is not automatically a materialized <see cref="NotableDate" />. It becomes visible to callers
/// only when an enabled <see cref="NotableDateRule" /> explicitly emits it.
/// </para>
/// </remarks>
internal readonly record struct CalculationAnchorCacheKey(
    string AnchorRuleName,
    int Year);