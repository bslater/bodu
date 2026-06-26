// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNumberHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies how <see cref="YamlSerializer" /> coerces numeric scalars when binding to integral targets.
/// </summary>
/// <remarks>
/// The default is <see cref="Strict" />, under which a floating-point scalar must be exactly integral and in range to
/// bind to an integer target. <see cref="AllowFloatToInteger" /> opts in to the lenient behavior of truncating a
/// fractional value, which discards data and should be selected deliberately.
/// </remarks>
public enum YamlNumberHandling
{
    /// <summary>
    /// Reject a non-integral or out-of-range numeric source when binding to an integer target. This is the default.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Allow a floating-point source to be truncated toward zero when binding to an integer target.
    /// </summary>
    AllowFloatToInteger = 1,
}
