// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Maps the format-neutral token names used by the shared serializer source (see
/// <c>Bodu.Text.Serialization/shared/</c>) to this format's <see cref="TomlTokenType" /> members. Each Bodu
/// text-format package defines the same constant names, so shared converters compare tokens without naming the
/// per-format vocabulary.
/// </summary>
internal static class FormatToken
{
    /// <summary>The token that carries string content.</summary>
    internal const TomlTokenType String = TomlTokenType.String;

    /// <summary>The token that carries integer content.</summary>
    internal const TomlTokenType Integer = TomlTokenType.Integer;
}
