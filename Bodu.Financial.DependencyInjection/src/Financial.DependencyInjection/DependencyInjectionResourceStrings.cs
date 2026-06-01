// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionResourceStrings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Centralizes the exception message templates used by the financial dependency-injection extension surface.
/// </summary>
/// <remarks>
/// Messages are exposed as <see langword="const" /> strings, matching the <c>DependencyInjectionResourceStrings</c>
/// convention used by <c>Bodu.Globalization.Calendar.DependencyInjection</c>.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Resource string identifiers follow the diagnostic-class_subject convention used by RESX-generated equivalents elsewhere in the solution.")]
internal static class DependencyInjectionResourceStrings
{
    /// <summary>
    /// A monetary-context name supplied to a builder method was empty or white space.
    /// </summary>
    internal const string Arg_Invalid_MonetaryContextNameBlank = "The monetary-context name must not be empty or white space.";
}
