// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Bundles the optional collaborators a <see cref="NotableDateService" /> consults when a document references custom
/// algorithms, collision resolution, adjustment handlers, trigger handlers, or code-first providers.
/// </summary>
/// <remarks>
/// <para>
/// Every property is independently optional. Where a property is <see langword="null" /> the service uses its built-in
/// behaviour for that concern. Construct an instance with an object initializer and pass it to
/// <see cref="NotableDateService(NotableDateResource, NotableDateServiceOptions)" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// NotableDateServiceOptions options = new()
/// {
///     Algorithms = new NotableDateAlgorithmRegistry().Register("contoso.harvest-moon", new HarvestMoonAlgorithm()),
///     Providers = new[] { myProvider },
/// };
///
/// NotableDateService service = new(resource, options);
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" />
public sealed class NotableDateServiceOptions
{
    /// <summary>
    /// Gets the custom algorithm registry consulted for keys the built-in catalogue does not recognize.
    /// </summary>
    /// <value>
    /// An <see cref="INotableDateAlgorithmRegistry" />, or <see langword="null" /> for built-in algorithms only.
    /// </value>
    public INotableDateAlgorithmRegistry? Algorithms { get; init; }

    /// <summary>
    /// Gets the custom same-day collision resolver consulted when the resource's collision policy is
    /// <see cref="CollisionPolicy.Custom" />.
    /// </summary>
    /// <value>An <see cref="INotableDateCollisionResolver" />, or <see langword="null" />.</value>
    public INotableDateCollisionResolver? CollisionResolver { get; init; }

    /// <summary>
    /// Gets the adjustment-handler registry consulted when an adjustment action is
    /// <see cref="AdjustmentAction.Custom" />.
    /// </summary>
    /// <value>An <see cref="IAdjustmentHandlerRegistry" />, or <see langword="null" />.</value>
    public IAdjustmentHandlerRegistry? Handlers { get; init; }

    /// <summary>
    /// Gets the trigger-handler registry consulted when an adjustment trigger is
    /// <see cref="AdjustmentTrigger.Custom" />.
    /// </summary>
    /// <value>An <see cref="IAdjustmentTriggerHandlerRegistry" />, or <see langword="null" />.</value>
    public IAdjustmentTriggerHandlerRegistry? TriggerHandlers { get; init; }

    /// <summary>
    /// Gets the code-first providers contributing finished occurrences alongside the resource's authored rules.
    /// </summary>
    /// <value>A sequence of <see cref="INotableDateProvider" />, or <see langword="null" /> for none.</value>
    public IEnumerable<INotableDateProvider>? Providers { get; init; }
}
