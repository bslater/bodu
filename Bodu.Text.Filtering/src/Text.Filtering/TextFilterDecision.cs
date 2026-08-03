// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterDecision.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Identifies the outcome of evaluating a value against a <see cref="TextFilter" />.
/// </summary>
public enum TextFilterDecision
{
    /// <summary>
    /// The value was accepted without any pattern matching it — in <see cref="TextFilterEvaluationMode.AnyMatch" />
    /// because the include set is empty, or in <see cref="TextFilterEvaluationMode.LastMatchWins" /> because no rule
    /// matched.
    /// </summary>
    IncludedByDefault = 0,

    /// <summary>
    /// The value was accepted because an include pattern matched it.
    /// </summary>
    Included = 1,

    /// <summary>
    /// The value was rejected because an exclude pattern matched it.
    /// </summary>
    Excluded = 2,

    /// <summary>
    /// The value was rejected because include patterns exist and none of them matched it. This outcome occurs only in
    /// <see cref="TextFilterEvaluationMode.AnyMatch" />.
    /// </summary>
    NotIncluded = 3,
}
