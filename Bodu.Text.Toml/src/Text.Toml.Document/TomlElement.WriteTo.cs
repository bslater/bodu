// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlElement.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Document;

/// <summary>
/// Provides the <see cref="WriteTo" /> surface of <see cref="TomlElement" />, which re-serializes an element's value to
/// a <see cref="Utf8TomlWriter" />.
/// </summary>
public readonly partial struct TomlElement
{
    /// <summary>
    /// Writes this element's value to the supplied writer at the writer's current position.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this element is the default value and belongs to no document.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    /// <remarks>
    /// A scalar element writes its decoded value, an array writes its elements in order, and a table writes its
    /// key/value pairs in stored order. The writer's own state machine governs where a value may be written, so a
    /// non-table element cannot be written at a TOML document root.
    /// </remarks>
    public void WriteTo(Utf8TomlWriter writer)
    {
        if (_document is null)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_DefaultElement);

        switch (ValueKind)
        {
            case TomlValueKind.String:
                writer.WriteString(GetString());
                return;

            case TomlValueKind.Integer:
                writer.WriteInteger(GetInt64());
                return;

            case TomlValueKind.Float:
                writer.WriteFloat(GetDouble());
                return;

            case TomlValueKind.Boolean:
                writer.WriteBoolean(GetBoolean());
                return;

            case TomlValueKind.OffsetDateTime:
                writer.WriteOffsetDateTime(GetDateTimeOffset());
                return;

            case TomlValueKind.LocalDateTime:
                writer.WriteLocalDateTime(GetDateTime());
                return;

            case TomlValueKind.LocalDate:
                writer.WriteLocalDate(GetDateOnly());
                return;

            case TomlValueKind.LocalTime:
                writer.WriteLocalTime(GetTimeOnly());
                return;

            case TomlValueKind.Array:
                writer.WriteStartArray();
                foreach (TomlElement element in EnumerateArray())
                    element.WriteTo(writer);

                writer.WriteEndArray();
                return;

            default:
                writer.WriteStartTable();
                foreach (TomlProperty property in EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }

                writer.WriteEndTable();
                return;
        }
    }
}
