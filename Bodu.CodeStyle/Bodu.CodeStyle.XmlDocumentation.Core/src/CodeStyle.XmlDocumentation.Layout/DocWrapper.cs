// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocWrapper.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
/// indivisible. When a single word atom exceeds the column budget the wrapper emits it on its own line and
/// allows the line to exceed the budget rather than corrupt the content.
/// </para>
/// <para>
/// The wrap strategy is natural greedy: pack as many atoms onto a line as fit within the content budget and
/// break at the last fitting word boundary. Clause-aware wrapping (breaking at <c>','</c>, <c>';'</c>,
/// <c>'.'</c>, or <c>':'</c>) is deliberately not applied — it caused multi-sentence paragraphs to fragment
/// at every clause even when the line could comfortably absorb more text.
/// </para>
/// <para>
/// Adjacent atoms with no whitespace between them (for example a trailing <c>'.'</c> immediately after an
/// inline <c>&lt;see /&gt;</c> reference) are treated as a single typographic unit and are never split across
/// a line boundary, even when the join exceeds the budget. There is no whitespace to absorb such a break and
/// the resulting orphan-punctuation line is wrong.
/// </para>
/// </remarks>
internal static class DocWrapper
{
    /// <summary>
    /// Wraps the given atom sequence to the supplied content-column budget.
    /// </summary>
    /// <param name="atoms">The list of atoms; whitespace atoms are <c>" "</c> singletons.</param>
    /// <param name="contentBudget">The maximum content length per line, excluding the documentation prefix and base indent.</param>
    /// <returns>An enumerable of physical content lines, one entry per output line.</returns>
    public static IEnumerable<string> Wrap(IReadOnlyList<string> atoms, int contentBudget)
    {
        if (atoms is null) throw new ArgumentNullException(nameof(atoms));
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), XmlDocResourceStrings.Arg_OutOfRange_ContentBudgetNotPositive);

        var lineAtoms = new List<LineAtom>();
        var lineLength = 0;
        var pendingWhitespace = false;

        foreach (var atom in atoms)
        {
            if (IsWhitespaceAtom(atom))
            {
                if (lineAtoms.Count > 0)
                {
                    pendingWhitespace = true;
                }

                continue;
            }

            var hasLeadingSpace = pendingWhitespace && lineAtoms.Count > 0;
            var addedLength = (hasLeadingSpace ? 1 : 0) + atom.Length;

            // Atoms with no preceding whitespace are typographically joined to the previous atom (e.g. a
            // trailing '.' after </see> or <see ... />) and must not be split across a line boundary even
            // when the join exceeds the budget — there is no whitespace to absorb the break and an orphan
            // punctuation line is wrong.
            if (lineAtoms.Count == 0 || lineLength + addedLength <= contentBudget || !hasLeadingSpace)
            {
                lineAtoms.Add(new LineAtom(atom, hasLeadingSpace));
                lineLength += addedLength;
                pendingWhitespace = false;
                continue;
            }

            // Natural greedy wrap: emit the current line at the last fitting word boundary, then start the
            // next line with the overflowing atom.
            yield return JoinAtoms(lineAtoms, 0, lineAtoms.Count);
            lineAtoms.Clear();
            lineAtoms.Add(new LineAtom(atom, hasLeadingSpace: false));
            lineLength = atom.Length;
            pendingWhitespace = false;
        }

        if (lineAtoms.Count > 0)
        {
            yield return JoinAtoms(lineAtoms, 0, lineAtoms.Count);
        }
    }

    private static string JoinAtoms(List<LineAtom> atoms, int start, int count)
    {
        var sb = new StringBuilder();
        for (var i = start; i < start + count; i++)
        {
            if (i > start && atoms[i].HasLeadingSpace)
            {
                sb.Append(' ');
            }

            sb.Append(atoms[i].Text);
        }

        return sb.ToString();
    }

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

    private readonly struct LineAtom
    {
        public LineAtom(string text, bool hasLeadingSpace)
        {
            this.Text = text;
            this.HasLeadingSpace = hasLeadingSpace;
        }

        public string Text { get; }

        public bool HasLeadingSpace { get; }
    }
}
