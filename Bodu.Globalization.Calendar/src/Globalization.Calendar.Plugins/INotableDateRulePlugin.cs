// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateRulePlugin.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Marks a notable-date plugin that contributes one or more <see cref="INotableDateRuleProvider" /> instances to the
/// host. A plugin that only contributes custom algorithms need not implement this interface.
/// </summary>
/// <remarks>
/// Rule providers returned from <see cref="GetRuleProviders" /> are added to the <see cref="NotableDateService" />'s
/// provider list and participate in the normal flatten pipeline. They are not granted any special precedence — locally
/// declared rules in the host's own resources continue to win on composite (Name, Territory) collision.
/// </remarks>
public interface INotableDateRulePlugin
    : INotableDatePlugin
{
    /// <summary>
    /// Returns the rule providers this plugin contributes. The enumeration is consumed once during service
    /// construction; plugin authors may defer heavy work until an individual provider's
    /// <see cref="INotableDateRuleProvider.LoadRules" /> is invoked.
    /// </summary>
    /// <returns>Zero or more rule providers. Never <see langword="null" />.</returns>
    IEnumerable<INotableDateRuleProvider> GetRuleProviders();
}
