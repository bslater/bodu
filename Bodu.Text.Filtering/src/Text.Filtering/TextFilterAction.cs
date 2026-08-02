// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterAction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Specifies whether a <see cref="TextFilterPattern" /> admits or rejects the values it matches.
/// </summary>
public enum TextFilterAction
{
    /// <summary>
    /// The pattern admits matching values into the filtered result.
    /// </summary>
    Include = 0,

    /// <summary>
    /// The pattern rejects matching values from the filtered result.
    /// </summary>
    Exclude = 1,
}
