// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlElement.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Provides the <see cref="WriteTo" /> surface of <see cref="YamlElement" />, which re-serializes an element's value to
/// a <see cref="Utf8YamlWriter" />.
/// </summary>
public readonly partial struct YamlElement
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
    /// A scalar element writes its decoded value, a sequence writes its items in order, and a mapping writes its
    /// key/value pairs in stored order. Aliases were resolved when the document was parsed, so the emitted text is the
    /// fully composed tree.
    /// </remarks>
    public void WriteTo(Utf8YamlWriter writer)
    {
        if (_document is null)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_DefaultElement);

        switch (ValueKind)
        {
            case YamlValueKind.String:
                writer.WriteString(GetString());
                return;

            case YamlValueKind.Integer:
                writer.WriteInteger(GetInt64());
                return;

            case YamlValueKind.Float:
                writer.WriteDouble(GetDouble());
                return;

            case YamlValueKind.Boolean:
                writer.WriteBoolean(GetBoolean());
                return;

            case YamlValueKind.Null:
                writer.WriteNull();
                return;

            case YamlValueKind.Sequence:
                writer.WriteStartSequence();
                foreach (YamlElement element in EnumerateSequence())
                    element.WriteTo(writer);

                writer.WriteEndSequence();
                return;

            default:
                writer.WriteStartMapping();
                foreach (YamlProperty property in EnumerateMapping())
                {
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }

                writer.WriteEndMapping();
                return;
        }
    }
}
