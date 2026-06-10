// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReverseStringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// A test converter that reverses a string on write and on read, so its application is observable in the captured
/// tokens while a round trip still reproduces the original value.
/// </summary>
public sealed class ReverseStringConverter
    : FormatConverter<string>
{
    /// <inheritdoc />
    public override string Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        return Reverse(reader.GetString());
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, string value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);
        writer.WriteString(Reverse(value));
    }

    /// <summary>
    /// Reverses the characters of a string.
    /// </summary>
    /// <param name="value">The string to reverse.</param>
    /// <returns>The reversed string.</returns>
    private static string Reverse(string value)
    {
        char[] characters = value.ToCharArray();
        Array.Reverse(characters);
        return new string(characters);
    }
}
