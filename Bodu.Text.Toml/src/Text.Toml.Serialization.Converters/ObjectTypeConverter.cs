// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectTypeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a value statically typed as <see cref="object" />: on write the value's runtime type selects the converter,
/// and on read the value surfaces as a <see cref="TomlElement" />.
/// </summary>
/// <remarks>
/// <para>
/// A value whose runtime type is exactly <see cref="object" /> carries no data, so it writes as an empty table. Any
/// other runtime type dispatches to that type's resolved converter, so a boxed scalar, collection, dictionary, or plain
/// object writes exactly as it would when statically typed. A <see langword="null" /> value writes nothing, matching
/// the omission of null members TOML's missing null form imposes.
/// </para>
/// <para>
/// On read the value's subtree is parsed into a <see cref="TomlElement" /> backed by an internal, garbage-collected
/// document, so the element never requires disposal.
/// </para>
/// </remarks>
internal sealed class ObjectTypeConverter
    : TomlConverter<object>
{
    /// <inheritdoc />
    public override object Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
        TomlDocument.ParseValue(ref reader).RootElement;

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, object value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        // A bare System.Object has no members and would otherwise resolve back to this converter and recurse.
        if (value.GetType() == typeof(object))
        {
            writer.WriteStartTable();
            writer.WriteEndTable();
            return;
        }

        options.GetConverter(value.GetType()).WriteAsObject(writer, value, options);
    }
}
