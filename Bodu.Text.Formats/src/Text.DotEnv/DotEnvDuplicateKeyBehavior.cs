// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDuplicateKeyBehavior.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Specifies how the DotEnv parser behaves when the same key name appears more than once in the source.
/// </summary>
public enum DotEnvDuplicateKeyBehavior
{
    /// <summary>
    /// The last occurrence of a duplicated key wins; earlier values are discarded. This is the default.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence of a duplicated key is retained; subsequent occurrences are silently ignored.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// Any duplicated key causes a <see cref="DotEnvFormatException" /> to be thrown.
    /// </summary>
    Disallowed = 2,
}
