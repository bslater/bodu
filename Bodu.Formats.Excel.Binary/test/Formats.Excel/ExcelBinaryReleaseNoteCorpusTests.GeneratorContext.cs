// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.GeneratorContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the state a generator body sets: the layout version and the settings its <c>@</c> items give.
    /// </summary>
    private sealed class GeneratorContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorContext" /> class.
        /// </summary>
        /// <param name="version">The version the records are laid out in.</param>
        public GeneratorContext(BiffVersion version) =>
            Version = version;

        /// <summary>
        /// Gets the version the records are laid out in.
        /// </summary>
        /// <value><see cref="BiffVersion.Biff5" /> or <see cref="BiffVersion.Biff8" />.</value>
        public BiffVersion Version { get; }

        /// <summary>
        /// Gets the code page BIFF5 text without a <c>cpN</c> prefix is encoded in.
        /// </summary>
        /// <value>The value of the last <c>@cp</c> setting; 1252 when there is none.</value>
        public int CodePage { get; private set; } = 1252;

        /// <summary>
        /// Gets the name the record stream is stored under.
        /// </summary>
        /// <value>The value of the last <c>@stream</c> setting, or <see langword="null" /> when there is none.</value>
        public string? StreamName { get; private set; }

        /// <summary>
        /// Gets the length the record stream is padded to with zero bytes.
        /// </summary>
        /// <value>The value of the last <c>@pad</c> setting; zero when there is none.</value>
        public int PadLength { get; private set; }

        /// <summary>
        /// Gets the extra streams the <c>@extra</c> settings add beside the record stream.
        /// </summary>
        /// <value>The name and length of each, in the order the settings give them.</value>
        public List<(string Name, int Length)> Extras { get; } = [];

        /// <summary>
        /// Gets the container patches the <c>@cfb</c> settings give.
        /// </summary>
        /// <value>The patches, in the order the settings give them.</value>
        public List<string> ContainerPatches { get; } = [];

        /// <summary>
        /// Gets a value indicating whether a <c>@container=none</c> setting asked for the bare record stream as the
        /// file.
        /// </summary>
        /// <value><see langword="true" /> when the setting is present.</value>
        public bool WithoutContainer { get; private set; }

        /// <summary>
        /// Applies one setting.
        /// </summary>
        /// <param name="setting">The setting, written <c>key=value</c> without its leading <c>@</c>.</param>
        /// <exception cref="FormatException">
        /// The setting is malformed or unknown, or its value is not valid.
        /// </exception>
        public void ApplySetting(string setting)
        {
            int equals = setting.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0)
                throw new FormatException($"The setting '@{setting}' is not written @key=value.");

            string value = setting[(equals + 1)..];
            switch (setting[..equals])
            {
                case "stream":
                    StreamName = CorpusEscapes.DecodeText(value);
                    break;

                case "cp":
                    CodePage = RecordBuilder.ParseInt32(value);
                    break;

                case "pad":
                    PadLength = RecordBuilder.ParseInt32(value);
                    break;

                case "extra":
                {
                    int colon = value.LastIndexOf(':');
                    if (colon <= 0)
                        throw new FormatException($"The setting '@extra={value}' is not written @extra=NAME:LENGTH.");

                    Extras.Add((CorpusEscapes.DecodeText(value[..colon]), RecordBuilder.ParseInt32(value[(colon + 1)..])));
                    break;
                }

                case "cfb":
                    ContainerPatcher.Check(value);
                    ContainerPatches.Add(value);
                    break;

                case "container":
                    WithoutContainer = value == "none" ? true : throw new FormatException($"The setting '@container={value}' is not @container=none.");
                    break;

                default:
                    throw new FormatException($"The setting '@{setting}' is not one the generators know.");
            }
        }
    }
}
