// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.IO.Pst;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalog row runs with, and maps them onto the mail-store and container reader
    /// options.
    /// </summary>
    /// <remarks>
    /// An option a row leaves out takes the reader's own default, so a row states only what its scenario changes.
    /// </remarks>
    private sealed record RowOptions
    {
        /// <summary>The parts of the Store rendering the <c>Detail</c> option can ask for.</summary>
        private static readonly string[] s_detailParts = ["properties", "recipients", "attachments", "bodies", "folders", "store", "associated"];

        /// <summary>The views the <c>View</c> option can select.</summary>
        private static readonly string[] s_views = ["Pc", "Tc", "RowIds", "Lookup", "Subnodes", "Data"];

        /// <summary>
        /// Gets the surface the input is read on.
        /// </summary>
        /// <value><c>Store</c> (the default) or <c>Nodes</c>.</value>
        public string Surface { get; init; } = "Store";

        /// <summary>
        /// Gets the validation level both readers apply.
        /// </summary>
        /// <value><see cref="PstValidationLevel.Compatible" /> by default.</value>
        public PstValidationLevel ValidationLevel { get; init; } = PstValidationLevel.Compatible;

        /// <summary>
        /// Gets a value indicating whether both readers omit values whose references do not resolve.
        /// </summary>
        /// <value><see langword="false" /> by default.</value>
        public bool OmitUnresolvableValues { get; init; }

        /// <summary>
        /// Gets a value indicating whether the mail store decompresses RTF bodies.
        /// </summary>
        /// <value><see langword="true" /> by default.</value>
        public bool DecompressRtf { get; init; } = true;

        /// <summary>
        /// Gets the block cache size either reader uses.
        /// </summary>
        /// <value>The size, or <see langword="null" /> for the reader's default.</value>
        public int? BlockCacheSize { get; init; }

        /// <summary>
        /// Gets the largest node data length either reader accepts.
        /// </summary>
        /// <value>The limit, or <see langword="null" /> for the reader's default.</value>
        public long? MaxNodeDataLength { get; init; }

        /// <summary>
        /// Gets the largest number of data-tree leaves the container accepts.
        /// </summary>
        /// <value>The limit, or <see langword="null" /> for the reader's default.</value>
        public int? MaxDataTreeLeaves { get; init; }

        /// <summary>
        /// Gets the deepest embedded-message nesting the mail store accepts.
        /// </summary>
        /// <value>The limit, or <see langword="null" /> for the reader's default.</value>
        public int? MaxEmbeddedMessageDepth { get; init; }

        /// <summary>
        /// Gets the largest decompressed RTF body the mail store accepts.
        /// </summary>
        /// <value>The limit, or <see langword="null" /> for the reader's default.</value>
        public int? MaxDecompressedRtfBytes { get; init; }

        /// <summary>
        /// Gets the largest attachment payload the mail store reads inline.
        /// </summary>
        /// <value>The limit, or <see langword="null" /> for the reader's default.</value>
        public int? MaxInlineAttachmentBytes { get; init; }

        /// <summary>
        /// Gets the optional parts of the Store rendering the row asks for.
        /// </summary>
        /// <value>
        /// Any of <c>properties</c>, <c>recipients</c>, <c>attachments</c>, <c>bodies</c>, <c>folders</c>, <c>store</c>
        /// and <c>associated</c>.
        /// </value>
        public IReadOnlyList<string> Detail { get; init; } = [];

        /// <summary>
        /// Gets the number of named-property identifiers, from <c>0x8000</c>, the Store rendering resolves.
        /// </summary>
        /// <value>Zero by default.</value>
        public int Names { get; init; }

        /// <summary>
        /// Gets the node the Nodes rendering's view reads.
        /// </summary>
        /// <value>The node identifier, or <see langword="null" /> for no view.</value>
        public uint? Node { get; init; }

        /// <summary>
        /// Gets the subnode of <see cref="Node" /> the view reads instead of the node itself.
        /// </summary>
        /// <value>The subnode identifier, or <see langword="null" /> for the node itself.</value>
        public uint? Subnode { get; init; }

        /// <summary>
        /// Gets the view the Nodes rendering adds.
        /// </summary>
        /// <value>
        /// One of <c>Pc</c>, <c>Tc</c>, <c>RowIds</c>, <c>Lookup</c>, <c>Subnodes</c> and <c>Data</c>, or
        /// <see langword="null" />.
        /// </value>
        public string? View { get; init; }

        /// <summary>
        /// Gets the row identifier the <c>Lookup</c> view looks up.
        /// </summary>
        /// <value>The row identifier, or <see langword="null" /> for another view.</value>
        public uint? Row { get; init; }

        /// <summary>
        /// Gets a value indicating whether every node line of the Nodes rendering carries the node's data.
        /// </summary>
        /// <value><see langword="false" /> by default.</value>
        public bool Data { get; init; }

        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">
        /// An option is malformed, unknown, has a value it does not accept, or does not apply to the selected surface.
        /// </exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions();
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "Surface" when value is "Store" or "Nodes" => options with { Surface = value },
                    "ValidationLevel" => options with { ValidationLevel = ParseValidationLevel(value) },
                    "OmitUnresolvableValues" => options with { OmitUnresolvableValues = ParseBoolean(name, value) },
                    "DecompressRtf" => options with { DecompressRtf = ParseBoolean(name, value) },
                    "BlockCacheSize" => options with { BlockCacheSize = ParseInt32(name, value, minimum: 0) },
                    "MaxNodeDataLength" => options with { MaxNodeDataLength = ParseInt64(name, value, minimum: 1) },
                    "MaxDataTreeLeaves" => options with { MaxDataTreeLeaves = ParseInt32(name, value, minimum: 1) },
                    "MaxEmbeddedMessageDepth" => options with { MaxEmbeddedMessageDepth = ParseInt32(name, value, minimum: 1) },
                    "MaxDecompressedRtfBytes" => options with { MaxDecompressedRtfBytes = ParseInt32(name, value, minimum: 1) },
                    "MaxInlineAttachmentBytes" => options with { MaxInlineAttachmentBytes = ParseInt32(name, value, minimum: 1) },
                    "Detail" => options with { Detail = ParseDetail(value) },
                    "Names" => options with { Names = ParseInt32(name, value, minimum: 0) },
                    "Node" => options with { Node = ParseIdentifier(name, value) },
                    "Subnode" => options with { Subnode = ParseIdentifier(name, value) },
                    "View" when s_views.Contains(value, StringComparer.Ordinal) => options with { View = value },
                    "Row" => options with { Row = ParseIdentifier(name, value) },
                    "Data" => options with { Data = ParseBoolean(name, value) },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            options.Validate();
            return options;
        }

        /// <summary>
        /// Determines whether the row asks for one optional part of the Store rendering.
        /// </summary>
        /// <param name="part">The part, as the <c>Detail</c> option names it.</param>
        /// <returns><see langword="true" /> when <see cref="Detail" /> holds <paramref name="part" />.</returns>
        public bool HasDetail(string part) =>
            Detail.Contains(part, StringComparer.Ordinal);

        /// <summary>
        /// Creates the mail-store reader options for the row.
        /// </summary>
        /// <returns>The reader options.</returns>
        public OutlookMailStoreReaderOptions ToReaderOptions()
        {
            var defaults = new OutlookMailStoreReaderOptions();

            return new OutlookMailStoreReaderOptions
            {
                ValidationLevel = ValidationLevel,
                OmitUnresolvableValues = OmitUnresolvableValues,
                DecompressRtf = DecompressRtf,
                BlockCacheSize = BlockCacheSize ?? defaults.BlockCacheSize,
                MaxNodeDataLength = MaxNodeDataLength ?? defaults.MaxNodeDataLength,
                MaxEmbeddedMessageDepth = MaxEmbeddedMessageDepth ?? defaults.MaxEmbeddedMessageDepth,
                MaxDecompressedRtfBytes = MaxDecompressedRtfBytes ?? defaults.MaxDecompressedRtfBytes,
                MaxInlineAttachmentBytes = MaxInlineAttachmentBytes ?? defaults.MaxInlineAttachmentBytes,
            };
        }

        /// <summary>
        /// Creates the container options for the row.
        /// </summary>
        /// <returns>The container options.</returns>
        public PstFileOptions ToFileOptions()
        {
            var defaults = new PstFileOptions();

            return new PstFileOptions
            {
                ValidationLevel = ValidationLevel,
                OmitUnresolvableValues = OmitUnresolvableValues,
                BlockCacheSize = BlockCacheSize ?? defaults.BlockCacheSize,
                MaxNodeDataLength = MaxNodeDataLength ?? defaults.MaxNodeDataLength,
                MaxDataTreeLeaves = MaxDataTreeLeaves ?? defaults.MaxDataTreeLeaves,
            };
        }

        /// <summary>
        /// Reads a validation level.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <returns>The level.</returns>
        /// <exception cref="FormatException">
        /// The value is not <c>Minimal</c>, <c>Compatible</c> or <c>Strict</c>.
        /// </exception>
        private static PstValidationLevel ParseValidationLevel(string value) =>
            value switch
            {
                "Minimal" => PstValidationLevel.Minimal,
                "Compatible" => PstValidationLevel.Compatible,
                "Strict" => PstValidationLevel.Strict,
                _ => throw new FormatException($"The option ValidationLevel takes Minimal, Compatible or Strict, not '{value}'."),
            };

        /// <summary>
        /// Reads a Boolean option value.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is neither <c>true</c> nor <c>false</c>.</exception>
        private static bool ParseBoolean(string name, string value) =>
            value switch
            {
                "true" => true,
                "false" => false,
                _ => throw new FormatException($"The option {name} takes true or false, not '{value}'."),
            };

        /// <summary>
        /// Reads a decimal option value that fits <see cref="int" />.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <param name="minimum">The smallest value the option accepts.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">
        /// The value is not decimal digits, or is below <paramref name="minimum" />.
        /// </exception>
        private static int ParseInt32(string name, string value, int minimum) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number >= minimum
                ? number
                : throw new FormatException($"The option {name} takes a decimal integer of at least {minimum}, not '{value}'.");

        /// <summary>
        /// Reads a decimal option value that fits <see cref="long" />.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <param name="minimum">The smallest value the option accepts.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">
        /// The value is not decimal digits, or is below <paramref name="minimum" />.
        /// </exception>
        private static long ParseInt64(string name, string value, long minimum) =>
            long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long number) && number >= minimum
                ? number
                : throw new FormatException($"The option {name} takes a decimal integer of at least {minimum}, not '{value}'.");

        /// <summary>
        /// Reads a node, subnode or row identifier written <c>0x</c> and hexadecimal digits.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The identifier.</returns>
        /// <exception cref="FormatException">
        /// The value is not <c>0x</c> followed by up to eight hexadecimal digits.
        /// </exception>
        private static uint ParseIdentifier(string name, string value) =>
            value.StartsWith("0x", StringComparison.Ordinal)
                && uint.TryParse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint identifier)
                ? identifier
                : throw new FormatException($"The option {name} takes 0x and hexadecimal digits, not '{value}'.");

        /// <summary>
        /// Reads the <c>Detail</c> option's comma-separated list.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <returns>The parts, in the order given.</returns>
        /// <exception cref="FormatException">An item is not a part the Store rendering has.</exception>
        private static string[] ParseDetail(string value)
        {
            string[] parts = value.Split(',');
            foreach (string part in parts)
            {
                if (!s_detailParts.Contains(part, StringComparer.Ordinal))
                    throw new FormatException($"The Detail item '{part}' is not one of {string.Join(", ", s_detailParts)}.");
            }

            return parts;
        }

        /// <summary>
        /// Checks that the options fit together: each surface's options appear only with that surface, a view is given
        /// with the node it reads, and a row identifier only with the <c>Lookup</c> view.
        /// </summary>
        /// <exception cref="FormatException">The options do not fit together.</exception>
        private void Validate()
        {
            bool nodes = Surface == "Nodes";
            if (nodes && (Detail.Count > 0 || Names > 0 || !DecompressRtf || MaxEmbeddedMessageDepth is not null || MaxDecompressedRtfBytes is not null || MaxInlineAttachmentBytes is not null))
                throw new FormatException("Detail, Names, DecompressRtf and the mail-store limits apply to the Store surface only.");

            if (!nodes && (Node is not null || Subnode is not null || View is not null || Data || MaxDataTreeLeaves is not null))
                throw new FormatException("Node, Subnode, View, Data and MaxDataTreeLeaves apply to the Nodes surface only.");

            if ((View is null) != (Node is null) || (Subnode is not null && Node is null))
                throw new FormatException("View is given with Node, and Subnode only with both.");

            if ((View == "Lookup") != (Row is not null))
                throw new FormatException("Row is given with View=Lookup, and only then.");
        }
    }
}
