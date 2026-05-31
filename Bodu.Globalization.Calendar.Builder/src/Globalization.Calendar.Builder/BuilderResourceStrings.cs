// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuilderResourceStrings.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Centralizes the diagnostic and exception message templates used by the notable-date builder pipeline so that wording
/// remains consistent across <see cref="NotableDateBuilder" />, <see cref="NotableDateRuleBuilder" /> and
/// <see cref="ObservanceAdjustmentBuilder" />.
/// </summary>
/// <remarks>
/// Messages are exposed as <see langword="const" /> strings rather than via a RESX-backed resource manager. The builder
/// API does not currently require localized diagnostics; if localization is later added, callers can replace this class
/// without affecting public API. Key names follow the <c>&lt;Group&gt;_&lt;ExceptionTypeShort&gt;_&lt;Condition&gt;</c>
/// convention used by every RESX-backed counterpart elsewhere in the solution.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Resource string identifiers follow the diagnostic-class_subject convention used by RESX-generated equivalents elsewhere in the solution.")]
internal static class BuilderResourceStrings
{
    /// <summary>
    /// An adjustment action was not set before <c>Build()</c> was invoked.
    /// </summary>
    internal const string Op_Invalid_AdjustmentActionMissingBuild = "An adjustment action must be set via Action() before building.";

    /// <summary>
    /// An adjustment action was not set before serialisation.
    /// </summary>
    internal const string Op_Invalid_AdjustmentActionMissingSerialise = "An adjustment action must be set via Action() before serialising.";

    /// <summary>
    /// An adjustment trigger was not set before <c>Build()</c> was invoked.
    /// </summary>
    internal const string Op_Invalid_AdjustmentTriggerMissingBuild = "An adjustment trigger must be set via When() before building.";

    /// <summary>
    /// An adjustment trigger was not set before serialisation.
    /// </summary>
    internal const string Op_Invalid_AdjustmentTriggerMissingSerialise = "An adjustment trigger must be set via When() before serialising.";

    /// <summary>
    /// A notable date entry contained no rules at <c>Build()</c> time.
    /// </summary>
    internal const string Op_Invalid_NotableDateRulesEmpty = "At least one rule must be added to the notable date entry '{0}'.";

    /// <summary>
    /// Two strategies were configured on the same rule.
    /// </summary>
    internal const string Op_Invalid_RuleStrategyAlreadySet = "A resolution strategy ('{0}') has already been selected for this rule; each rule must specify exactly one strategy.";

    /// <summary>
    /// No resolution strategy was selected before <c>Build()</c> was invoked.
    /// </summary>
    internal const string Op_Invalid_RuleStrategyMissingBuild = "A resolution strategy must be selected (Fixed, DayOfWeekInMonth, OffsetFromAnchor, or Algorithm) before building the rule for '{0}'.";

    /// <summary>
    /// No resolution strategy was selected before serialisation.
    /// </summary>
    internal const string Op_Invalid_RuleStrategyMissingSerialise = "A resolution strategy must be selected before serialising the rule for '{0}'.";

    /// <summary>
    /// A strategy value was not recognised by the builder.
    /// </summary>
    internal const string Op_NotSupported_Strategy = "Unsupported strategy: {0}.";
}
