// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScalarCoercion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Provides the cross-kind scalar coercion the YAML converters share: YAML's implicit typing resolves a plain scalar to
/// its most specific kind, so a converter targeting a text-parsed type accepts any scalar kind and re-reads it as its
/// invariant text.
/// </summary>
internal static class ScalarCoercion
{
    /// <summary>
    /// Produces the string form of the scalar at the reader's current position.
    /// </summary>
    /// <param name="reader">The reader positioned on the scalar.</param>
    /// <returns>The scalar's text.</returns>
    /// <exception cref="YamlSerializationException">The current token is not a scalar.</exception>
    internal static string ReaderScalarText(ref Utf8YamlReader reader) => reader.TokenType switch
    {
        YamlTokenType.String => reader.GetString(),
        YamlTokenType.Integer => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
        YamlTokenType.Float => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
        YamlTokenType.Boolean => reader.GetBoolean() ? "true" : "false",
        YamlTokenType.Null => string.Empty,
        _ => throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedScalar),
    };
}
