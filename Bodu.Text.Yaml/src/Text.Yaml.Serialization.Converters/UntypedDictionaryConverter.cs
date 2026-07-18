// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UntypedDictionaryConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a dictionary the generic dictionary factory does not claim — a non-generic <see cref="IDictionary" /> such
/// as <see cref="Hashtable" />, or a generic dictionary whose key type has no round-trippable text form — as a YAML
/// mapping whose keys are each entry key's invariant text.
/// </summary>
/// <remarks>
/// Distinct keys whose string forms collide (for example the integer <c>1</c> and the string <c>"1"</c>) are rejected
/// with <see cref="YamlSerializationException" />, so the serializer never emits a document its own parser would reject
/// as a duplicate key. Entry values dispatch on their runtime type; a null value writes the null scalar.
/// </remarks>
internal sealed class UntypedDictionaryConverter
    : YamlConverter<IDictionary>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeof(IDictionary).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc />
    public override IDictionary Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (reader.TokenType == YamlTokenType.Null)
            return null!;

        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedMapping);

        if (typeToConvert.IsInterface || typeToConvert.IsAbstract || typeToConvert.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlTypeNotInstantiable, typeToConvert));
        }

        var dictionary = (IDictionary)Activator.CreateInstance(typeToConvert)!;
        YamlConverter valueConverter = options.GetConverter(typeof(object));
        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();
            dictionary[name] = ChildBinder.Read(ref reader, valueConverter, typeof(object), options, name);
        }

        return dictionary;
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, IDictionary value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        YamlWriteStack? state = writer.WriteStack;
        if (state is not null && !state.TryEnterReference(value))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, value.GetType()));
            return;
        }

        try
        {
            writer.WriteStartMapping();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in value)
            {
                if (state is { HasFailure: true })
                    break;

                string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!seen.Add(key))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlDuplicateDictionaryKey, key));
                }

                state?.PushPath(key);
                writer.WritePropertyName(key);

                if (entry.Value is null)
                    writer.WriteNull();
                else
                    options.GetConverter(entry.Value.GetType()).WriteAsObject(writer, entry.Value, options);

                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            writer.WriteEndMapping();
        }
        finally
        {
            state?.ExitReference(value);
        }
    }
}
