// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentWriter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Emits an <see cref="IniDocument" /> to a <see cref="TextWriter" /> according to a
/// <see cref="BoduConfigurationWriteOptions" />. Used by
/// <see cref="BoduConfigurationDocument.Save(IniDocument, string, BoduConfigurationWriteOptions?)" /> and its
/// stream/reader overloads.
/// </summary>
/// <remarks>
/// Unlike <see cref="Ini.Format(IniDocument)" /> — which always emits trivia using the INI defaults — this writer
/// honors the Bodu-specific options: <see cref="BoduConfigurationWriteOptions.KeyValueSeparator" />,
/// <see cref="BoduConfigurationWriteOptions.NewLine" />, <see cref="BoduConfigurationWriteOptions.PreserveComments" />,
/// <see cref="BoduConfigurationWriteOptions.WriteInlineComments" />, and
/// <see cref="BoduConfigurationWriteOptions.InsertBlankLineBetweenSections" />.
/// </remarks>
internal static class ConfigurationDocumentWriter
{
    internal static void Write(IniDocument document, TextWriter writer, BoduConfigurationWriteOptions options)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(options);

        bool wroteAny = WriteSection(document.GlobalSection, writer, options, isGlobal: true);

        foreach (IniSection section in document.Sections)
        {
            if (wroteAny && options.InsertBlankLineBetweenSections)
                writer.Write(options.NewLine);

            wroteAny = WriteSection(section, writer, options, isGlobal: false) || wroteAny;
        }
    }

    private static bool WriteSection(IniSection section, TextWriter writer, BoduConfigurationWriteOptions options, bool isGlobal)
    {
        bool wroteAny = false;

        if (options.PreserveComments)
        {
            foreach (IniComment comment in section.LeadingComments)
            {
                writer.Write(comment.Prefix);
                writer.Write(comment.Text);
                writer.Write(options.NewLine);
                wroteAny = true;
            }
        }

        if (!isGlobal && section.Name.Length > 0)
        {
            writer.Write('[');
            writer.Write(section.Name);
            writer.Write(']');
            writer.Write(options.NewLine);
            wroteAny = true;
        }

        foreach (IniEntry entry in section.Entries)
        {
            if (options.PreserveComments)
            {
                foreach (IniComment leading in entry.LeadingComments)
                {
                    writer.Write(leading.Prefix);
                    writer.Write(leading.Text);
                    writer.Write(options.NewLine);
                }
            }

            writer.Write(entry.Key);
            writer.Write(options.KeyValueSeparator);
            writer.Write(entry.Value);

            if (options.WriteInlineComments && entry.InlineComment is { } inline)
            {
                writer.Write(' ');
                writer.Write(inline.Prefix);
                writer.Write(inline.Text);
            }

            writer.Write(options.NewLine);
            wroteAny = true;
        }

        return wroteAny;
    }
}
