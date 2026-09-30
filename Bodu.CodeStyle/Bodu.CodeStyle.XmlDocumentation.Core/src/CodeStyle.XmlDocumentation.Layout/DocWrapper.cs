// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocWrapper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.CodeStyle.XmlDocumentation.Layout;

/// <summary>
/// Wraps a sequence of atomic chunks into one or more physical lines without splitting any individual atom.
/// </summary>
/// <remarks>
/// <para>
/// The input is a list of strings produced by <see cref="DocLayout" />: word atoms interleaved with optional
/// single-space whitespace atoms. The wrapper treats whitespace atoms as break opportunities and word atoms as
/// indivisible. When a single word atom exceeds the column budget the wrapper emits it on its own line and allows the
/// line to exceed the budget rather than corrupt the content.
/// </para>
/// <para>
/// The wrap strategy is natural greedy: pack as many atoms onto a line as fit within the content budget and break at
/// the last fitting word boundary. Clause-aware wrapping is deliberately not applied.
/// </para>
/// <para>
/// One exception: DocFX renders documentation text as Markdown, so a line the wrapper opens with an atom Markdown reads
/// as the start of a block (see <see cref="IsMarkdownBlockMarker" />) would become a list, heading or quote. When the
/// overflowing atom is such a marker, the break moves before the preceding word, which then opens the next line; when
/// the line holds no such word, the marker stays on it, over budget. A marker at the start of the text is the author's
/// and is left where it is.
/// </para>
/// <para>
/// Adjacent atoms with no whitespace between them (for example a trailing <c>'.'</c> immediately after an inline
/// <c>&lt;see /&gt;</c> reference) are treated as a single typographic unit and are never split across a line boundary,
/// even when the join exceeds the budget.
/// </para>
/// <para>
/// An atom that itself contains line breaks (a tag preserved verbatim under
/// <see cref="XmlDocFormatOptions.PreserveXmlTagAttributes" />) is emitted across multiple lines: its first segment
/// continues the current line, its interior segments stand alone, and its final segment seeds the next line so
/// following prose continues from it.
/// </para>
/// </remarks>
internal static class DocWrapper
{
    /// <summary>
    /// Wraps the given atom sequence to the supplied content-column budget.
    /// </summary>
    /// <param name="atoms">The list of atoms; whitespace atoms are <c>" "</c> singletons.</param>
    /// <param name="contentBudget">
    /// The maximum content length per line, excluding the documentation prefix and base indent.
    /// </param>
    /// <returns>An enumerable of physical content lines, one entry per output line.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="atoms" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="contentBudget" /> is non-positive.
    /// </exception>
    public static IEnumerable<string> Wrap(IReadOnlyList<string> atoms, int contentBudget)
    {
        if (atoms is null) throw new ArgumentNullException(nameof(atoms));
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), XmlDocResourceStrings.Arg_OutOfRange_ContentBudgetNotPositive);

        var lines = new List<string>();
        var current = new StringBuilder();
        var currentHasContent = false;
        string? pendingWhitespace = null;

        // The offset in `current` of the whitespace before each word after the first: the places the running line
        // could still be broken.
        var breaks = new List<int>();

        foreach (var atom in atoms)
        {
            if (IsWhitespaceAtom(atom))
            {
                if (currentHasContent)
                {
                    pendingWhitespace = atom;
                }

                continue;
            }

            if (atom.IndexOf('\n') >= 0)
            {
                AppendMultiLineAtom(atom, lines, current, ref currentHasContent, ref pendingWhitespace);
                breaks.Clear();
                continue;
            }

            var leadingWhitespace = currentHasContent ? pendingWhitespace ?? string.Empty : string.Empty;
            var addedLength = leadingWhitespace.Length + atom.Length;

            // Atoms with no preceding whitespace are typographically joined to the previous atom (e.g. a trailing
            // '.' after </see>) and must not be split across a line boundary even when the join exceeds budget.
            if (!currentHasContent || current.Length + addedLength <= contentBudget || leadingWhitespace.Length == 0)
            {
                AppendAtom(current, breaks, leadingWhitespace, atom);
                currentHasContent = true;
                pendingWhitespace = null;
                continue;
            }

            if (IsMarkdownBlockMarker(atom))
            {
                // The marker must not open the next line: break before the last word that can open it instead, or,
                // when there is none, keep the marker on this line over budget.
                var carryBreak = FindCarryBreak(current, breaks);
                if (carryBreak >= 0)
                {
                    CarryTailToNextLine(lines, current, breaks, carryBreak);
                }

                AppendAtom(current, breaks, leadingWhitespace, atom);
                pendingWhitespace = null;
                continue;
            }

            // Natural greedy wrap: emit the current line at the last fitting word boundary, then start the next
            // line with the overflowing atom.
            lines.Add(current.ToString());
            current.Clear();
            breaks.Clear();
            current.Append(atom);
            currentHasContent = true;
            pendingWhitespace = null;
        }

        if (currentHasContent)
        {
            lines.Add(current.ToString());
        }

        return lines;
    }

    /// <summary>
    /// Determines whether a word, placed at the start of a line, would make Markdown begin a block rather than continue
    /// the paragraph.
    /// </summary>
    /// <param name="word">The word, as it appears in the documentation source.</param>
    /// <returns>
    /// <see langword="true" /> for a bullet (<c>-</c>, <c>+</c>, <c>*</c>), a setext underline or thematic break (a run
    /// of <c>-</c>, <c>=</c> or <c>*</c>), an ATX heading (one to six <c>#</c>), a block quote (a word beginning with
    /// <c>&gt;</c>, raw or as the <c>&amp;gt;</c> entity), or an ordered list starting at one (<c>1.</c>, <c>1)</c>);
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Only an ordered list that starts at one can interrupt a paragraph, so <c>2.</c> or <c>10)</c> opening a
    /// continuation line is harmless and is not a marker.
    /// </remarks>
    internal static bool IsMarkdownBlockMarker(string word)
    {
        if (word.Length == 0) return false;

        if (word == "+" || word == "1." || word == "1)") return true;
        if (IsRunOf(word, '-') || IsRunOf(word, '=') || IsRunOf(word, '*')) return true;
        if (word.Length <= 6 && IsRunOf(word, '#')) return true;

        return word[0] == '>' || word.StartsWith("&gt;", StringComparison.Ordinal);
    }

    /// <summary>
    /// Appends a word to the running line, recording the break opportunity its leading whitespace creates.
    /// </summary>
    /// <param name="current">The builder for the line currently being assembled.</param>
    /// <param name="breaks">The break opportunities recorded on the running line.</param>
    /// <param name="leadingWhitespace">The whitespace separating the word from the line so far; empty when joined.</param>
    /// <param name="atom">The word atom to append.</param>
    private static void AppendAtom(StringBuilder current, List<int> breaks, string leadingWhitespace, string atom)
    {
        if (leadingWhitespace.Length > 0)
        {
            breaks.Add(current.Length);
        }

        current.Append(leadingWhitespace).Append(atom);
    }

    /// <summary>
    /// Finds the latest break on the running line whose following word can open a line, so that breaking there
    /// carries that word, and everything after it, onto the next line.
    /// </summary>
    /// <param name="current">The builder for the line currently being assembled.</param>
    /// <param name="breaks">The break opportunities recorded on the running line, in order.</param>
    /// <returns>The offset of the chosen break, or <c>-1</c> when every candidate word is itself a block marker.</returns>
    private static int FindCarryBreak(StringBuilder current, List<int> breaks)
    {
        for (var i = breaks.Count - 1; i >= 0; i--)
        {
            var wordStart = SkipWhitespace(current, breaks[i]);
            var wordEnd = i + 1 < breaks.Count ? breaks[i + 1] : current.Length;
            if (!IsMarkdownBlockMarker(current.ToString(wordStart, wordEnd - wordStart)))
            {
                return breaks[i];
            }
        }

        return -1;
    }

    /// <summary>
    /// Emits the running line up to a break and keeps the text after it as the start of the next line.
    /// </summary>
    /// <param name="lines">The accumulated output lines.</param>
    /// <param name="current">The builder for the line currently being assembled; on return, the carried text.</param>
    /// <param name="breaks">The break opportunities on the running line; on return, those within the carried text.</param>
    /// <param name="carryBreak">The offset of the break to emit the line at.</param>
    private static void CarryTailToNextLine(List<string> lines, StringBuilder current, List<int> breaks, int carryBreak)
    {
        var tailStart = SkipWhitespace(current, carryBreak);
        var tail = current.ToString(tailStart, current.Length - tailStart);

        lines.Add(current.ToString(0, carryBreak));
        current.Clear().Append(tail);

        var carried = new List<int>();
        foreach (var offset in breaks)
        {
            if (offset > carryBreak)
            {
                carried.Add(offset - tailStart);
            }
        }

        breaks.Clear();
        breaks.AddRange(carried);
    }

    /// <summary>
    /// Returns the offset of the first character at or after <paramref name="offset" /> that is not a space or tab.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <param name="offset">The offset to start from.</param>
    /// <returns>The offset of the first non-whitespace character, or the text's length when there is none.</returns>
    private static int SkipWhitespace(StringBuilder text, int offset)
    {
        while (offset < text.Length && (text[offset] == ' ' || text[offset] == '\t'))
        {
            offset++;
        }

        return offset;
    }

    /// <summary>
    /// Determines whether a word consists of one character repeated.
    /// </summary>
    /// <param name="word">The word to test; must not be empty.</param>
    /// <param name="character">The character the word must consist of.</param>
    /// <returns><see langword="true" /> if every character of the word is <paramref name="character" />.</returns>
    private static bool IsRunOf(string word, char character)
    {
        for (var i = 0; i < word.Length; i++)
        {
            if (word[i] != character)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Appends an atom that itself contains line breaks, emitting its first segment on the current line, its interior
    /// segments as standalone lines, and seeding the running line with its final segment.
    /// </summary>
    /// <param name="atom">The multi-line atom, with segments separated by <c>'\n'</c>.</param>
    /// <param name="lines">The accumulated output lines.</param>
    /// <param name="current">The builder for the line currently being assembled.</param>
    /// <param name="currentHasContent">
    /// A reference flag indicating whether the running line already holds content; updated on return.
    /// </param>
    /// <param name="pendingWhitespace">
    /// A reference to the pending separating whitespace, if any; cleared on return.
    /// </param>
    private static void AppendMultiLineAtom(string atom, List<string> lines, StringBuilder current, ref bool currentHasContent, ref string? pendingWhitespace)
    {
        var segments = atom.Split('\n');

        // The first segment continues the current line (attaching to preceding prose through any pending space).
        if (currentHasContent && pendingWhitespace is not null)
        {
            current.Append(pendingWhitespace);
        }

        current.Append(segments[0]);
        lines.Add(current.ToString());
        current.Clear();
        pendingWhitespace = null;

        // Interior segments stand alone, preserving the authored layout verbatim.
        for (var i = 1; i < segments.Length - 1; i++)
        {
            lines.Add(segments[i]);
        }

        // The final segment seeds the running line so following atoms continue from it.
        var last = segments[segments.Length - 1];
        current.Append(last);
        currentHasContent = last.Length > 0;
    }

    /// <summary>
    /// Determines whether an atom consists solely of spaces and tabs and therefore represents a break opportunity
    /// rather than content.
    /// </summary>
    /// <param name="atom">The atom to test.</param>
    /// <returns>
    /// <see langword="true" /> if the atom is empty or contains only horizontal whitespace; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool IsWhitespaceAtom(string atom)
    {
        if (atom.Length == 0) return true;

        for (var i = 0; i < atom.Length; i++)
        {
            if (atom[i] != ' ' && atom[i] != '\t')
            {
                return false;
            }
        }

        return true;
    }
}
