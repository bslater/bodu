// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateRuleProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies <see cref="NotableDateRule" /> instances to a <see cref="NotableDateService" /> from an underlying
/// authoring source such as an embedded XML resource, JSON document, database, or external configuration system.
/// </summary>
/// <remarks>
/// <para>
/// Rule providers are loaded once per <see cref="NotableDateService" /> instance. To express runtime-only modifications
/// such as disabling a holiday or adding a one-off observance, use an <see cref="INotableDateRuleOverrideProvider" />
/// alongside the base providers.
/// </para>
/// <para>
/// The built-in implementation is <see cref="XmlResourceNotableDateRuleProvider" />, which loads rules from an embedded
/// XML resource and recursively resolves cherry-pick directives across linked resource files.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// A minimal in-memory implementation for testing or dynamic rule sets:
/// </para>
/// <code>
///<![CDATA[
/// public sealed class InMemoryRuleProvider : INotableDateRuleProvider
/// {
///     private readonly IReadOnlyList<NotableDateRule> _rules;
///
///     public InMemoryRuleProvider(IReadOnlyList<NotableDateRule> rules) => _rules = rules;
///
///     public IEnumerable<NotableDateRule> LoadRules() => _rules;
/// }
///]]>
/// </code>
/// </example>
public interface INotableDateRuleProvider
{
    /// <summary>
    /// Loads every <see cref="NotableDateRule" /> exposed by this provider.
    /// </summary>
    /// <returns>The notable date rules.</returns>
    /// <exception cref="System.Exception">Thrown if the underlying source cannot be loaded or is invalid.</exception>
    IEnumerable<NotableDateRule> LoadRules();
}
