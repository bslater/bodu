// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICalculationAnchorResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves calculation anchors used by derived notable-date rules.
/// </summary>
/// <remarks>
/// <para>
/// Calculation anchors are resolved independently from the public notable-date output window. This allows a rule such
/// as <c>Start of Lent</c> to calculate <c>Easter Sunday</c> even when Easter Sunday itself is outside the caller's
/// requested date window.
/// </para>
/// </remarks>
internal interface ICalculationAnchorResolver
{
    /// <summary>
    /// Resolves the calculation anchor represented by the specified rule name and civil year.
    /// </summary>
    /// <param name="anchorRuleName">The name of the rule that provides the anchor date.</param>
    /// <param name="year">The civil year for which the anchor should be resolved.</param>
    /// <returns>
    /// The resolved anchor date, or <see langword="null" /> when the anchor rule does not apply in
    /// <paramref name="year" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="anchorRuleName" /> is <see langword="null" />, empty, or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The named anchor rule cannot be found, or resolving the rule fails due to invalid rule configuration.
    /// </exception>
    DateTime? Resolve(string anchorRuleName, int year);
}