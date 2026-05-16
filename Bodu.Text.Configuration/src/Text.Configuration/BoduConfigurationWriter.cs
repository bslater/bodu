// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Bodu.Text.Configuration;

/// <summary>
/// Emits a <see cref="BoduConfigurationDocument" /> to a <see cref="TextWriter" /> according to the supplied
/// <see cref="BoduConfigurationWriteOptions" />.
/// </summary>
internal sealed class BoduConfigurationWriter
{
    private readonly BoduConfigurationWriteOptions _options;

    internal BoduConfigurationWriter(BoduConfigurationWriteOptions options)
    {
        this._options = options;
    }

    internal void Write(BoduConfigurationDocument document, TextWriter writer)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(writer);

        bool wroteAny = WriteSection(document.Preamble, writer, isPreamble: true);

        foreach (BoduConfigurationSection section in document.Sections)
        {
            if (wroteAny && this._options.InsertBlankLineBetweenSections)
                writer.Write(this._options.NewLine);

            wroteAny = WriteSection(section, writer, isPreamble: false) || wroteAny;
        }
    }

    private bool WriteSection(BoduConfigurationSection section, TextWriter writer, bool isPreamble)
    {
        bool wroteAny = false;

        if (this._options.PreserveComments)
        {
            foreach (BoduConfigurationComment comment in section.LeadingComments)
            {
                writer.Write(comment.Prefix);
                writer.Write(comment.Text);
                writer.Write(this._options.NewLine);
                wroteAny = true;
            }
        }

        if (!isPreamble && section.Pattern is not null)
        {
            writer.Write('[');
            writer.Write(section.Pattern);
            writer.Write(']');
            writer.Write(this._options.NewLine);
            wroteAny = true;
        }

        foreach (BoduConfigurationProperty property in section.Properties)
        {
            if (this._options.PreserveComments)
            {
                foreach (BoduConfigurationComment leading in property.LeadingComments)
                {
                    writer.Write(leading.Prefix);
                    writer.Write(leading.Text);
                    writer.Write(this._options.NewLine);
                }
            }

            writer.Write(property.RawKey);
            writer.Write(this._options.KeyValueSeparator);
            writer.Write(property.Value);

            if (this._options.WriteInlineComments && property.InlineComment.HasValue)
            {
                BoduConfigurationComment inline = property.InlineComment.Value;
                writer.Write(' ');
                writer.Write(inline.Prefix);
                writer.Write(inline.Text);
            }

            writer.Write(this._options.NewLine);
            wroteAny = true;
        }

        return wroteAny;
    }
}
