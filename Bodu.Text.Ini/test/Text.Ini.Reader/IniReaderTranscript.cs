// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReaderTranscript.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Records what <see cref="Utf8IniReader" /> reports, token by token, when it reads an input in one final block, in
/// blocks cut at chosen offsets, or resuming from a new reader after every token, so that reading in blocks can be held
/// to the single pass.
/// </summary>
/// <remarks>
/// Each entry names a token's kind, its text, its line number, the offset in the whole input at which it begins, and
/// the offset in the whole input of <see cref="Utf8IniReader.BytesConsumed" />; the last entry describes the end of the
/// read or the <see cref="IniFormatException" /> it ended with, message, line and offset included.
/// </remarks>
internal static class IniReaderTranscript
{
    /// <summary>
    /// Gets the cut lists that split an input of a length every way the resumable reader must survive: in two at every
    /// offset, and into chunks of every size, or of a sample of sizes and offsets when the input is long.
    /// </summary>
    /// <param name="length">The length of the input in bytes.</param>
    /// <returns>The cut lists, each an ascending list of offsets at which a block ends.</returns>
    internal static IEnumerable<int[]> Cuts(int length)
    {
        int stride = length <= 300 ? 1 : (length / 150) + 1;
        for (int cut = 0; cut <= length; cut += stride)
            yield return [cut];

        IEnumerable<int> sizes = length <= 300
            ? Enumerable.Range(1, Math.Max(0, length - 1))
            : [1, 2, 3, 5, 8, 13, 64, 256];

        foreach (int size in sizes)
            yield return [.. Enumerable.Range(1, (length - 1) / size).Select(index => index * size)];
    }

    /// <summary>
    /// Asserts that reading an input in every split <see cref="Cuts(int)" /> generates, and with a new reader after
    /// every token, reports exactly what a single read of the input in one final block reports.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="context">The name of the case, which a failure message begins with.</param>
    internal static void AssertEverySplitMatches(byte[] input, IniReaderOptions options, string context)
    {
        List<string> expected = ReadWhole(input, options);

        foreach (int[] cuts in Cuts(input.Length))
            CollectionAssert.AreEqual(expected, ReadInBlocks(input, options, cuts), $"{context}: blocks ending at {string.Join(",", cuts)}");

        CollectionAssert.AreEqual(expected, ReadResumingAfterEveryToken(input, options), $"{context}: resumed after every token");
    }

    /// <summary>
    /// Reads an input in one final block.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The transcript.</returns>
    internal static List<string> ReadWhole(byte[] input, IniReaderOptions options) =>
        ReadInBlocks(input, options, []);

    /// <summary>
    /// Reads an input in blocks that end at each cut, the last block, from the last cut to the end, being final, and
    /// carries the unconsumed bytes and the reader state from each block to the next.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="cuts">The ascending offsets at which the blocks before the final one end.</param>
    /// <returns>The transcript.</returns>
    internal static List<string> ReadInBlocks(byte[] input, IniReaderOptions options, IReadOnlyList<int> cuts) =>
        ReadInBlocks(input, new IniReaderState(options), cuts);

    /// <summary>
    /// Reads an input in blocks that end at each cut, starting from a given state, the last block, from the last cut to
    /// the end, being final.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="state">The state the first block's reader starts from.</param>
    /// <param name="cuts">The ascending offsets at which the blocks before the final one end.</param>
    /// <returns>The transcript.</returns>
    internal static List<string> ReadInBlocks(byte[] input, IniReaderState state, IReadOnlyList<int> cuts)
    {
        var transcript = new List<string>();
        byte[] carried = [];
        int fed = 0;

        try
        {
            for (int block = 0; block <= cuts.Count; block++)
            {
                bool isFinal = block == cuts.Count;
                int end = isFinal ? input.Length : cuts[block];
                long bufferStart = fed - carried.Length;
                byte[] buffer = [.. carried, .. input.AsSpan(fed, end - fed)];
                fed = end;

                var reader = new Utf8IniReader(buffer, isFinal, state);
                while (reader.Read())
                    transcript.Add(Describe(ref reader, bufferStart));

                if (isFinal)
                {
                    transcript.Add(DescribeEnd(ref reader, bufferStart));
                    break;
                }

                state = reader.CurrentState;
                carried = buffer[reader.BytesConsumed..];
            }
        }
        catch (IniFormatException ex)
        {
            transcript.Add(Describe(ex));
        }

        return transcript;
    }

    /// <summary>
    /// Reads an input in one final block, but after every token builds a new reader over the unconsumed bytes from the
    /// current reader's state, and reads on with that one.
    /// </summary>
    /// <param name="input">The UTF-8 input.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The transcript.</returns>
    internal static List<string> ReadResumingAfterEveryToken(byte[] input, IniReaderOptions options)
    {
        var transcript = new List<string>();
        var state = new IniReaderState(options);
        int offset = 0;

        try
        {
            while (true)
            {
                var reader = new Utf8IniReader(input.AsSpan(offset), isFinalBlock: true, state);
                if (!reader.Read())
                {
                    transcript.Add(DescribeEnd(ref reader, offset));
                    break;
                }

                transcript.Add(Describe(ref reader, offset));
                state = reader.CurrentState;
                offset += reader.BytesConsumed;
            }
        }
        catch (IniFormatException ex)
        {
            transcript.Add(Describe(ex));
        }

        return transcript;
    }

    /// <summary>
    /// Describes the current token of a reader.
    /// </summary>
    /// <param name="reader">The reader, positioned on a token.</param>
    /// <param name="bufferStart">The offset in the whole input of the first byte of the reader's block.</param>
    /// <returns>The token's transcript entry.</returns>
    private static string Describe(ref Utf8IniReader reader, long bufferStart)
    {
        string text = reader.TokenType is IniTokenType.SectionHeader or IniTokenType.PropertyName or IniTokenType.String or IniTokenType.Comment
            ? reader.GetString()
            : "-";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{reader.TokenType}|{text}|{reader.LineNumber}|{reader.TokenOffset}|{bufferStart + reader.BytesConsumed}");
    }

    /// <summary>
    /// Describes the state of a reader once it has read to the end of the input.
    /// </summary>
    /// <param name="reader">
    /// The reader, after <see cref="Utf8IniReader.Read" /> returned <see langword="false" />.
    /// </param>
    /// <param name="bufferStart">The offset in the whole input of the first byte of the reader's block.</param>
    /// <returns>The end's transcript entry.</returns>
    private static string DescribeEnd(ref Utf8IniReader reader, long bufferStart) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"end|{reader.TokenType}|{reader.LineNumber}|{reader.TokenOffset}|{bufferStart + reader.BytesConsumed}");

    /// <summary>
    /// Describes the exception a read ended with.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The exception's transcript entry.</returns>
    private static string Describe(IniFormatException exception) =>
        string.Create(CultureInfo.InvariantCulture, $"{exception.GetType().Name}|{exception.Message}|{exception.LineNumber}|{exception.Offset}");
}
