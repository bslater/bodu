// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvParseOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Controls dialect-specific parsing behaviour for <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and related methods.
/// </summary>
public readonly struct DotEnvParseOptions
{
    /// <summary>
    /// Gets a <see cref="DotEnvParseOptions" /> instance initialised with all default values.
    /// </summary>
    /// <returns>A default <see cref="DotEnvParseOptions" /> value.</returns>
    public static readonly DotEnvParseOptions Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvParseOptions" /> struct with all properties set to their
    /// defaults.
    /// </summary>
    public DotEnvParseOptions()
    {
    }

    /// <summary>
    /// Gets a value indicating how the parser behaves when the same key appears more than once in the source.
    /// </summary>
    /// <returns>
    /// A <see cref="DotEnvDuplicateKeyBehavior" /> value. The default is
    /// <see cref="DotEnvDuplicateKeyBehavior.LastWins" />.
    /// </returns>
    public DotEnvDuplicateKeyBehavior DuplicateKeyBehavior { get; init; } = DotEnvDuplicateKeyBehavior.LastWins;

    /// <summary>
    /// Gets a value indicating whether lines that begin with the <c>export </c> prefix are accepted. When
    /// <see langword="true" />, the literal word <c>export</c> followed by one or more spaces is stripped before the
    /// key is parsed. Only one level of <c>export</c> is consumed per line.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if <c>export </c> lines are accepted; otherwise, <see langword="false" />. The default
    /// is <see langword="true" />.
    /// </returns>
    public bool AllowExportPrefix { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inline comments are recognised in unquoted values. When <see langword="true" />,
    /// a <c>#</c> character that is preceded by at least one whitespace character truncates an unquoted value at that
    /// point; trailing whitespace is then also trimmed.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if inline comments are stripped; otherwise, <see langword="false" />. The default is
    /// <see langword="true" />.
    /// </returns>
    public bool AllowInlineComments { get; init; } = true;
}
