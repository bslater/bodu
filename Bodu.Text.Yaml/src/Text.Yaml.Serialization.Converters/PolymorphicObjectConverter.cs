// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolymorphicObjectConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a value declared as an interface or abstract class: on write the value's runtime type selects the
/// converter, so a polymorphic member serializes exactly as its concrete instance would; reading is rejected because
/// the declared type cannot be instantiated (the collection and dictionary interfaces are claimed by their factories
/// earlier in the resolution order and never reach this converter).
/// </summary>
internal sealed class PolymorphicObjectConverter
    : YamlConverter<object>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsInterface || typeToConvert.IsAbstract;
    }

    /// <inheritdoc />
    public override object Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return null!;

        throw new YamlSerializationException(string.Format(
            CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlTypeNotInstantiable, typeToConvert));
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, object value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        options.GetConverter(value.GetType()).WriteAsObject(writer, value, options);
    }
}
