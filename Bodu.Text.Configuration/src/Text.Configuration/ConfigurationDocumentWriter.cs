// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Emits an <see cref="IniDocumentBase" /> to a <see cref="TextWriter" /> according to a
/// <see cref="ConfigurationWriteOptions" />. Used by
/// <see cref="ConfigurationDocument.Save(IniDocumentBase, string, ConfigurationWriteOptions?)" /> and its stream/reader
/// overloads.
/// </summary>
/// <remarks>
/// Unlike <see cref="Bodu.Text.Ini.Ini.Format(IniDocument)" /> — which always emits trivia using the INI defaults —
/// this writer honors the Bodu-specific formatting options exposed by <see cref="ConfigurationWriteOptions" />.
/// </remarks>
internal static class ConfigurationDocumentWriter
{
    /// <summary>
    /// Writes <paramref name="document" /> to <paramref name="writer" /> according to <paramref name="options" />.
    /// </summary>
    /// <param name="document">The document to emit.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">The write options that govern separators, comments, and section spacing.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document" />, <paramref name="writer" />, or <paramref name="options" /> is
    /// <see langword="null" />.
    /// </exception>
    internal static void Write(IniDocumentBase document, TextWriter writer, ConfigurationWriteOptions options)
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

    /// <summary>
    /// Writes a single section — its leading comments, header line, and entries — and reports whether any output was
    /// produced.
    /// </summary>
    /// <param name="section">The section to emit.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">The write options that govern separators, comments, and section spacing.</param>
    /// <param name="isGlobal">
    /// <see langword="true" /> when <paramref name="section" /> is the document's global section, whose header line is
    /// suppressed.
    /// </param>
    /// <returns><see langword="true" /> when any line was written; otherwise, <see langword="false" />.</returns>
    private static bool WriteSection(IniSection section, TextWriter writer, ConfigurationWriteOptions options, bool isGlobal)
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
