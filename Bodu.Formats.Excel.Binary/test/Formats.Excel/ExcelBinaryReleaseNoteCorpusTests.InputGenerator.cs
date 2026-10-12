// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.InputGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Builds the record stream a generator expression describes.
    /// </summary>
    /// <remarks>
    /// <c>gen:Stream(A | B | ...)</c> writes the substreams <c>A</c>, <c>B</c>, ... in order, each a list of record
    /// constructors and <c>@</c> settings separated by white space. <c>gen:Sheet(G | S)</c> is shorthand for
    /// <c>gen:Stream(BOF(globals) G SHEET(1,'Sheet1') EOF | BOF(sheet) S EOF)</c>, and <c>gen:Sheet(S)</c> for the same
    /// with no globals items. A <c>|</c> or <c>,</c> inside parentheses or quotes does not separate.
    /// </remarks>
    private static class InputGenerator
    {
        /// <summary>
        /// Builds the input a generator expression describes.
        /// </summary>
        /// <param name="input">The row's <c>input</c> field.</param>
        /// <param name="version">The version to lay the records out in.</param>
        /// <returns>The generated input.</returns>
        /// <exception cref="FormatException"><paramref name="input" /> is not a valid generator expression.</exception>
        public static GeneratedInput Generate(string input, BiffVersion version)
        {
            if (!input.StartsWith("gen:", StringComparison.Ordinal))
                throw new FormatException("The input is not a generator expression, gen:Stream(...) or gen:Sheet(...).");

            if (!CorpusEscapes.TryDecode(input, out _, out string? error))
                throw new FormatException(error);

            int open = input.IndexOf('(', StringComparison.Ordinal);
            if (open < 0 || input[^1] != ')' || FindClose(input, open) != input.Length - 1)
                throw new FormatException("A generator expression is written gen:<Name>(<body>).");

            string body = input[(open + 1)..^1];
            var context = new GeneratorContext(version);
            List<List<GeneratorItem>> substreams = input["gen:".Length..open] switch
            {
                "Stream" => [.. SplitTopLevel(body, '|').Select(part => ParseItems(part, context))],
                "Sheet" => ExpandSheet(body, context),
                string name => throw new FormatException($"The generator '{name}' is not Stream or Sheet."),
            };

            byte[] stream = Assemble(substreams, context);
            if (stream.Length < context.PadLength)
                Array.Resize(ref stream, context.PadLength);

            return new GeneratedInput(stream, context.StreamName ?? (version == BiffVersion.Biff5 ? "Book" : "Workbook"))
            {
                Extras = context.Extras,
                ContainerPatches = context.ContainerPatches,
                WithoutContainer = context.WithoutContainer,
            };
        }

        /// <summary>
        /// Splits text at a separator that stands outside quotes and parentheses.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>The parts, untrimmed; one part when the separator does not occur.</returns>
        /// <exception cref="FormatException">A quote is not closed.</exception>
        public static List<string> SplitTopLevel(string text, char separator)
        {
            var parts = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\'')
                    i = FindQuoteEnd(text, i);
                else if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;
                else if (c == separator && depth == 0)
                {
                    parts.Add(text[start..i]);
                    start = i + 1;
                }
            }

            parts.Add(text[start..]);
            return parts;
        }

        /// <summary>
        /// Expands a <c>Sheet</c> body into its globals substream and its worksheet substream.
        /// </summary>
        /// <param name="body">
        /// The body: the worksheet items, optionally preceded by globals items and a <c>|</c>.
        /// </param>
        /// <param name="context">The generator context, which receives the settings the body gives.</param>
        /// <returns>
        /// <c>BOF(globals)</c>, the globals items, <c>SHEET(1,'Sheet1')</c> and <c>EOF</c>; then <c>BOF(sheet)</c>, the
        /// worksheet items and <c>EOF</c>.
        /// </returns>
        /// <exception cref="FormatException">
        /// The body holds more than one <c>|</c>, or an item is malformed.
        /// </exception>
        private static List<List<GeneratorItem>> ExpandSheet(string body, GeneratorContext context)
        {
            List<string> parts = SplitTopLevel(body, '|');
            if (parts.Count > 2)
                throw new FormatException("A Sheet body holds at most one '|'.");

            List<GeneratorItem> globals = parts.Count == 2 ? ParseItems(parts[0], context) : [];
            List<GeneratorItem> worksheet = ParseItems(parts[^1], context);
            return
            [
                [new GeneratorItem("BOF", ["globals"]), .. globals, new GeneratorItem("SHEET", ["1", "'Sheet1'"]), new GeneratorItem("EOF", [])],
                [new GeneratorItem("BOF", ["sheet"]), .. worksheet, new GeneratorItem("EOF", [])],
            ];
        }

        /// <summary>
        /// Writes the substreams in order, resolving each <c>SHEET(k, ...)</c> to the offset of substream <c>k</c>.
        /// </summary>
        /// <param name="substreams">The substreams' items.</param>
        /// <param name="context">The generator context.</param>
        /// <returns>The record stream.</returns>
        /// <exception cref="FormatException">An item is malformed.</exception>
        /// <remarks>
        /// No record's length depends on the offsets <c>SHEET</c> resolves, so one measuring pass fixes every
        /// substream's offset before the records are written.
        /// </remarks>
        private static byte[] Assemble(List<List<GeneratorItem>> substreams, GeneratorContext context)
        {
            var starts = new long[substreams.Count];
            long position = 0;
            for (var i = 0; i < substreams.Count; i++)
            {
                starts[i] = position;
                foreach (GeneratorItem item in substreams[i])
                    position += RecordBuilder.Build(item, context, substreamStarts: null).Length;
            }

            using var output = new MemoryStream();
            foreach (List<GeneratorItem> substream in substreams)
            {
                foreach (GeneratorItem item in substream)
                    output.Write(RecordBuilder.Build(item, context, starts));
            }

            return output.ToArray();
        }

        /// <summary>
        /// Parses the items of one substream, applying its settings as they are found.
        /// </summary>
        /// <param name="text">The substream's text.</param>
        /// <param name="context">The generator context, which receives the settings.</param>
        /// <returns>The record constructors, in order.</returns>
        /// <exception cref="FormatException">An item or a setting is malformed.</exception>
        private static List<GeneratorItem> ParseItems(string text, GeneratorContext context)
        {
            var items = new List<GeneratorItem>();
            var i = 0;
            while (true)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;

                if (i >= text.Length)
                    return items;

                if (text[i] == '@')
                {
                    int end = i;
                    while (end < text.Length && !char.IsWhiteSpace(text[end]))
                        end++;

                    context.ApplySetting(text[(i + 1)..end]);
                    i = end;
                    continue;
                }

                int start = i;
                while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] == '_'))
                    i++;

                if (i == start)
                    throw new FormatException($"The item text '{text}' holds an unexpected '{text[i]}'.");

                string name = text[start..i].ToUpperInvariant();
                List<string> arguments = [];
                if (i < text.Length && text[i] == '(')
                {
                    int close = FindClose(text, i);
                    arguments = [.. SplitTopLevel(text[(i + 1)..close], ',').Select(argument => argument.Trim())];
                    if (arguments is [{ Length: 0 }])
                        arguments.Clear();

                    i = close + 1;
                }

                items.Add(new GeneratorItem(name, arguments));
            }
        }

        /// <summary>
        /// Finds the parenthesis that closes the one at a position.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="open">The position of the opening parenthesis.</param>
        /// <returns>The position of the matching closing parenthesis.</returns>
        /// <exception cref="FormatException">A quote or the parenthesis is not closed.</exception>
        private static int FindClose(string text, int open)
        {
            var depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\'')
                    i = FindQuoteEnd(text, i);
                else if (c == '(')
                    depth++;
                else if (c == ')' && --depth == 0)
                    return i;
            }

            throw new FormatException("A parenthesis is not closed.");
        }

        /// <summary>
        /// Finds the quote that closes the one at a position.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="open">The position of the opening quote.</param>
        /// <returns>The position of the closing quote.</returns>
        /// <exception cref="FormatException">The quote is not closed.</exception>
        private static int FindQuoteEnd(string text, int open)
        {
            int end = text.IndexOf('\'', open + 1);
            return end >= 0 ? end : throw new FormatException("A quote is not closed.");
        }
    }
}
