// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocWrapper.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Wrap point selection prefers the latest clause boundary (a word ending in <c>','</c>, <c>';'</c>, <c>'.'</c>,
/// or <c>':'</c>) when available; if no clause boundary exists in the line the wrapper falls back to the latest
/// word boundary.
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
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), "Content budget must be positive.");

        List<LineAtom> lineAtoms = new List<LineAtom>();
        int lineLength = 0;
        bool pendingWhitespace = false;

        foreach (string atom in atoms)
        {
            if (IsWhitespaceAtom(atom))
            {
                if (lineAtoms.Count > 0)
                {
                    pendingWhitespace = true;
                }

                continue;
            }

            bool hasLeadingSpace = pendingWhitespace && lineAtoms.Count > 0;
            int addedLength = (hasLeadingSpace ? 1 : 0) + atom.Length;

            if (lineAtoms.Count == 0 || lineLength + addedLength <= contentBudget)
            {
                lineAtoms.Add(new LineAtom(atom, hasLeadingSpace));
                lineLength += addedLength;
                pendingWhitespace = false;
                continue;
            }

            int? bestBreak = FindBestBreak(lineAtoms);
            if (bestBreak.HasValue)
            {
                yield return JoinAtoms(lineAtoms, 0, bestBreak.Value);

                int splitIndex = bestBreak.Value;
                List<LineAtom> remainder = new List<LineAtom>(lineAtoms.Count - splitIndex + 1);
                remainder.Add(new LineAtom(lineAtoms[splitIndex].Text, hasLeadingSpace: false));
                for (int i = splitIndex + 1; i < lineAtoms.Count; i++)
                {
                    remainder.Add(lineAtoms[i]);
                }

                remainder.Add(new LineAtom(atom, hasLeadingSpace: pendingWhitespace && remainder.Count > 0));

                lineAtoms.Clear();
                lineAtoms.AddRange(remainder);
                lineLength = ComputeLineLength(lineAtoms);
            }
            else
            {
                yield return JoinAtoms(lineAtoms, 0, lineAtoms.Count);
                lineAtoms.Clear();
                lineAtoms.Add(new LineAtom(atom, hasLeadingSpace: false));
                lineLength = atom.Length;
            }

            pendingWhitespace = false;
        }

        if (lineAtoms.Count > 0)
        {
            yield return JoinAtoms(lineAtoms, 0, lineAtoms.Count);
        }
    }

    private static int? FindBestBreak(List<LineAtom> atoms)
    {
        // Prefer the latest clause boundary (a word ending in ", ; . :"). When no clause boundary exists in the
        // current line, return null so the caller falls back to natural greedy wrap (emit the current line
        // as-is, start the next line with the candidate atom).
        int? latestClause = null;
        for (int i = 1; i < atoms.Count; i++)
        {
            if (!atoms[i].HasLeadingSpace)
            {
                continue;
            }

            if (EndsWithClausePunctuation(atoms[i - 1].Text))
            {
                latestClause = i;
            }
        }

        return latestClause;
    }

    private static bool EndsWithClausePunctuation(string word)
    {
        if (word.Length == 0)
        {
            return false;
        }

        char last = word[word.Length - 1];
        return last == ',' || last == ';' || last == '.' || last == ':';
    }

    private static int ComputeLineLength(List<LineAtom> atoms)
    {
        int total = 0;
        foreach (LineAtom atom in atoms)
        {
            if (atom.HasLeadingSpace)
            {
                total += 1;
            }

            total += atom.Text.Length;
        }

        return total;
    }

    private static string JoinAtoms(List<LineAtom> atoms, int start, int count)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = start; i < start + count; i++)
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

        for (int i = 0; i < atom.Length; i++)
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
