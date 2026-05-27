// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectionResourceStrings.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Centralizes the exception message templates used by the dependency-injection extension surface so that wording
/// remains consistent across <see cref="ServiceCollectionExtensions" /> and
/// <see cref="NotableDateServiceBuilderExtensions" />.
/// </summary>
/// <remarks>
/// Messages are exposed as <see langword="const" /> strings rather than via a RESX-backed resource manager — matching
/// the <c>BuilderResourceStrings</c> convention used by <c>Bodu.Globalization.Calendar.Builder</c>. Key names follow
/// the <c>&lt;Group&gt;_&lt;ExceptionTypeShort&gt;_&lt;Condition&gt;</c> convention used by every RESX-backed
/// counterpart elsewhere in the solution. If localization is later added, callers can replace this class without
/// affecting public API.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Resource string identifiers follow the diagnostic-class_subject convention used by RESX-generated equivalents elsewhere in the solution.")]
internal static class DependencyInjectionResourceStrings
{
    /// <summary>
    /// A sequence-typed parameter (for example, the <c>providers</c> argument to
    /// <see cref="NotableDateServiceBuilderExtensions.AddRuleProviders" />) contained a <see langword="null" />
    /// element.
    /// </summary>
    internal const string Arg_Invalid_ProviderSequenceContainsNullElement = "The provider sequence must not contain null elements.";
}
