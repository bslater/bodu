// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.IO.Compound;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with: the surface that renders it, and the reader options it is read
    /// with.
    /// </summary>
    /// <param name="Surface">What renders the input: <c>Message</c>, <c>Conveniences</c> or <c>CompressedRtf</c>.</param>
    /// <param name="Reader">The reader options; the documented defaults for every option the row leaves out.</param>
    private sealed record RowOptions(string Surface, OutlookMessageReaderOptions Reader)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        /// <remarks>
        /// Every option but <c>Surface</c> sets the <see cref="OutlookMessageReaderOptions" /> property of the same name.
        /// </remarks>
        public static RowOptions Parse(string text)
        {
            var defaults = new OutlookMessageReaderOptions();
            string surface = "Message";
            CompoundValidationLevel validationLevel = defaults.ValidationLevel;
            CompoundReadStrategy readStrategy = defaults.ReadStrategy;
            bool decompressRtf = defaults.DecompressRtf;
            int maxEmbeddedMessageDepth = defaults.MaxEmbeddedMessageDepth;
            int maxDecompressedRtfBytes = defaults.MaxDecompressedRtfBytes;
            int maxInlineAttachmentBytes = defaults.MaxInlineAttachmentBytes;

            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                switch (name)
                {
                    case "Surface" when value is "Message" or "Conveniences" or "CompressedRtf":
                        surface = value;
                        break;

                    case "ValidationLevel":
                        validationLevel = value switch
                        {
                            "Compatible" => CompoundValidationLevel.Compatible,
                            "Strict" => CompoundValidationLevel.Strict,
                            "Minimal" => CompoundValidationLevel.Minimal,
                            _ => throw new FormatException($"The option {name} takes Compatible, Strict or Minimal, not '{value}'."),
                        };
                        break;

                    case "ReadStrategy":
                        readStrategy = value switch
                        {
                            "Buffered" => CompoundReadStrategy.Buffered,
                            "Streaming" => CompoundReadStrategy.Streaming,
                            "Auto" => CompoundReadStrategy.Auto,
                            _ => throw new FormatException($"The option {name} takes Buffered, Streaming or Auto, not '{value}'."),
                        };
                        break;

                    case "DecompressRtf":
                        decompressRtf = value switch
                        {
                            "true" => true,
                            "false" => false,
                            _ => throw new FormatException($"The option {name} takes true or false, not '{value}'."),
                        };
                        break;

                    case "MaxEmbeddedMessageDepth":
                        maxEmbeddedMessageDepth = ParsePositive(name, value);
                        break;

                    case "MaxDecompressedRtfBytes":
                        maxDecompressedRtfBytes = ParsePositive(name, value);
                        break;

                    case "MaxInlineAttachmentBytes":
                        maxInlineAttachmentBytes = ParsePositive(name, value);
                        break;

                    default:
                        throw new FormatException($"The option {name}={value} is not one this catalogue maps.");
                }
            }

            var reader = new OutlookMessageReaderOptions
            {
                ValidationLevel = validationLevel,
                ReadStrategy = readStrategy,
                DecompressRtf = decompressRtf,
                MaxEmbeddedMessageDepth = maxEmbeddedMessageDepth,
                MaxDecompressedRtfBytes = maxDecompressedRtfBytes,
                MaxInlineAttachmentBytes = maxInlineAttachmentBytes,
            };
            return new RowOptions(surface, reader);
        }

        /// <summary>
        /// Reads a positive whole-number option value.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not a positive whole number in decimal digits.</exception>
        private static int ParsePositive(string name, string value) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) && result > 0
                ? result
                : throw new FormatException($"The option {name} takes a positive whole number, not '{value}'.");
    }
}
