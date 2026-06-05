// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithmRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides lookup of registered <see cref="INotableDateAlgorithm" /> implementations by stable string key.
/// </summary>
/// <remarks>
/// <para>
/// The registry decouples a <see cref="NotableDateRule" /> from a CLR <see cref="Type" />: rules reference algorithms
/// by short identifier (for example <c>"easter-sunday"</c> or <c>"lunar-new-year"</c>) and the registry resolves them
/// to a concrete instance supplied by dependency injection or test code. This makes definitions resilient to assembly
/// renames and allows algorithms to declare constructor dependencies.
/// </para>
/// <para>
/// The built-in implementation is <see cref="NotableDateAlgorithmRegistry" />, which accepts pre-constructed algorithm
/// instances and looks them up case-insensitively by key.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Register algorithms and pass the registry to <see cref="NotableDateService" />:
/// </para>
/// <code>
///<![CDATA[
/// NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
///     .Register("easter-sunday", new GregorianEasterSundayNotableDateProvider())
///     .Register("orthodox-easter", new OrthodoxEasterSundayNotableDateProvider());
///
/// NotableDateService service = new NotableDateService(
///     ruleProviders: new[] { new XmlResourceNotableDateRuleProvider(
///         "MyApp/Calendar/Resources/rules.xml",
///         new ResourcePathResolver()) },
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
///     options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
///]]>
/// </code>
/// </example>
public interface INotableDateAlgorithmRegistry
{
    /// <summary>
    /// Attempts to retrieve the <see cref="INotableDateAlgorithm" /> registered against the specified key.
    /// </summary>
    /// <param name="key">The registry key. Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="algorithm">
    /// When this method returns <see langword="true" />, contains the resolved algorithm.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an algorithm is registered for <paramref name="key" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    bool TryGet(string key, out INotableDateAlgorithm algorithm);

    /// <summary>
    /// Indicates whether an algorithm is registered against the specified key.
    /// </summary>
    /// <param name="key">The registry key.</param>
    /// <returns><see langword="true" /> if an algorithm is registered; otherwise <see langword="false" />.</returns>
    bool Contains(string key);
}
