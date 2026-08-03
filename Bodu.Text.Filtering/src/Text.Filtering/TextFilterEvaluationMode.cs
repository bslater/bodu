// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterEvaluationMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Specifies how a <see cref="TextFilter" /> combines its patterns when deciding whether a value is accepted.
/// </summary>
public enum TextFilterEvaluationMode
{
    /// <summary>
    /// Patterns form two unordered sets (the Ant / MSBuild model): a value is accepted when the include set is empty or
    /// at least one include pattern matches, and no exclude pattern matches. Declaration order does not affect the
    /// outcome, which lets the filter evaluate the cheapest patterns first.
    /// </summary>
    AnyMatch = 0,

    /// <summary>
    /// Patterns form one ordered rule list (the gitignore model): the last matching rule's action decides the outcome,
    /// and a value that matches no rule is included. A later include can re-admit a value a previous exclude rejected.
    /// </summary>
    LastMatchWins = 1,
}
