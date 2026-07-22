// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializer.Factory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini;

public static partial class IniSerializer
{
    /// <summary>
    /// Serializes the specified value as one INI section using a section factory instead of reflection.
    /// </summary>
    /// <typeparam name="TSection">The section type.</typeparam>
    /// <param name="sectionName">The section name, or an empty string to write the entries as global keys.</param>
    /// <param name="value">The section value to serialize.</param>
    /// <param name="factory">The section factory that supplies the entries.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The INI text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sectionName" />, <paramref name="value" />, or <paramref name="factory" /> is
    /// <see langword="null" />.
    /// </exception>
    public static string SerializeSection<TSection>(string sectionName, TSection value, IIniSectionFactory<TSection> factory, IniSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        SerializeSection(buffer, sectionName, value, factory, options);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified value as one INI section to the supplied buffer writer using a section factory instead
    /// of reflection.
    /// </summary>
    /// <typeparam name="TSection">The section type.</typeparam>
    /// <param name="destination">The buffer writer that receives the INI bytes.</param>
    /// <param name="sectionName">The section name, or an empty string to write the entries as global keys.</param>
    /// <param name="value">The section value to serialize.</param>
    /// <param name="factory">The section factory that supplies the entries.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" />, <paramref name="sectionName" />, <paramref name="value" />, or
    /// <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    public static void SerializeSection<TSection>(IBufferWriter<byte> destination, string sectionName, TSection value, IIniSectionFactory<TSection> factory, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfNull(sectionName);
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(factory);

        IniSerializerOptions effective = options ?? IniSerializerOptions.Default;
        effective.MakeReadOnly();

        var writer = new Utf8IniWriter(destination);
        if (sectionName.Length > 0)
            writer.WriteSectionHeader(sectionName);

        foreach (KeyValuePair<string, string> entry in factory.GetEntries(value))
        {
            writer.WritePropertyName(entry.Key);
            writer.WriteString(entry.Value);
        }

        writer.Flush();
    }

    /// <summary>
    /// Deserializes one section of the specified INI text using a section factory instead of reflection.
    /// </summary>
    /// <typeparam name="TSection">The section type.</typeparam>
    /// <param name="text">The INI source text.</param>
    /// <param name="sectionName">The section name, or an empty string to bind the document's global keys.</param>
    /// <param name="factory">The section factory that binds the entries.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized section.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" />, <paramref name="sectionName" />, or <paramref name="factory" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">Thrown when the text is not valid INI.</exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document does not contain a section named <paramref name="sectionName" />.
    /// </exception>
    public static TSection DeserializeSection<TSection>(string text, string sectionName, IIniSectionFactory<TSection> factory, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return DeserializeSection(Encoding.UTF8.GetBytes(text).AsSpan(), sectionName, factory, options);
    }

    /// <summary>
    /// Deserializes one section of the specified UTF-8 INI bytes using a section factory instead of reflection.
    /// </summary>
    /// <typeparam name="TSection">The section type.</typeparam>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="sectionName">The section name, or an empty string to bind the document's global keys.</param>
    /// <param name="factory">The section factory that binds the entries.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The deserialized section.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sectionName" /> or <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    /// <exception cref="IniSerializationException">
    /// Thrown when the document does not contain a section named <paramref name="sectionName" />.
    /// </exception>
    public static TSection DeserializeSection<TSection>(ReadOnlySpan<byte> utf8Ini, string sectionName, IIniSectionFactory<TSection> factory, IniSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(sectionName);
        ThrowHelper.ThrowIfNull(factory);

        IniSerializerOptions effective = options ?? IniSerializerOptions.Default;
        effective.MakeReadOnly();

        IniNormalizedDocument document = IniNormalizedDocument.Parse(utf8Ini, IniReaderOptions.Default, effective.ToDocumentOptions());

        if (sectionName.Length == 0)
            return factory.Create(document.GlobalEntries);

        foreach (IniNormalizedSection section in document.Sections)
        {
            if (string.Equals(section.Name, sectionName, StringComparison.Ordinal))
                return factory.Create(section.Entries);
        }

        throw new IniSerializationException(string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Op_Invalid_IniSectionNotFound, sectionName));
    }
}
