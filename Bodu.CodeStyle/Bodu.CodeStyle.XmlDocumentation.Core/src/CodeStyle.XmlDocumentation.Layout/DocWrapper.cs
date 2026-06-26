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
/// indivisible. When a single word atom exceeds the column budget the wrapper emits it on its own line and
/// allows the line to exceed the budget rather than corrupt the content.
/// </para>
/// <para>
/// The wrap strategy is natural greedy: pack as many atoms onto a line as fit within the content budget and
/// break at the last fitting word boundary. Clause-aware wrapping is deliberately not applied.
/// </para>
/// <para>
/// Adjacent atoms with no whitespace between them (for example a trailing <c>'.'</c> immediately after an inline
/// <c>&lt;see /&gt;</c> reference) are treated as a single typographic unit and are never split across a line
/// boundary, even when the join exceeds the budget.
/// </para>
/// <para>
/// An atom that itself contains line breaks (a tag preserved verbatim under
/// <see cref="XmlDocFormatOptions.PreserveXmlTagAttributes" />) is emitted across multiple lines: its first
/// segment continues the current line, its interior segments stand alone, and its final segment seeds the next
/// line so following prose continues from it.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="atoms" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="contentBudget" /> is non-positive.</exception>
    public static IEnumerable<string> Wrap(IReadOnlyList<string> atoms, int contentBudget)
    {
        if (atoms is null) throw new ArgumentNullException(nameof(atoms));
        if (contentBudget <= 0) throw new ArgumentOutOfRangeException(nameof(contentBudget), XmlDocResourceStrings.Arg_OutOfRange_ContentBudgetNotPositive);

        var lines = new List<string>();
        var current = new StringBuilder();
        var currentHasContent = false;
        string? pendingWhitespace = null;

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
                continue;
            }

            var leadingWhitespace = currentHasContent ? pendingWhitespace ?? string.Empty : string.Empty;
            var addedLength = leadingWhitespace.Length + atom.Length;

            // Atoms with no preceding whitespace are typographically joined to the previous atom (e.g. a trailing
            // '.' after </see>) and must not be split across a line boundary even when the join exceeds budget.
            if (!currentHasContent || current.Length + addedLength <= contentBudget || leadingWhitespace.Length == 0)
            {
                current.Append(leadingWhitespace).Append(atom);
                currentHasContent = true;
                pendingWhitespace = null;
                continue;
            }

            // Natural greedy wrap: emit the current line at the last fitting word boundary, then start the next
            // line with the overflowing atom.
            lines.Add(current.ToString());
            current.Clear();
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
    /// Appends an atom that itself contains line breaks, emitting its first segment on the current line, its
    /// interior segments as standalone lines, and seeding the running line with its final segment.
    /// </summary>
    /// <param name="atom">The multi-line atom, with segments separated by <c>'\n'</c>.</param>
    /// <param name="lines">The accumulated output lines.</param>
    /// <param name="current">The builder for the line currently being assembled.</param>
    /// <param name="currentHasContent">A reference flag indicating whether the running line already holds content; updated on return.</param>
    /// <param name="pendingWhitespace">A reference to the pending separating whitespace, if any; cleared on return.</param>
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
    /// Determines whether an atom consists solely of spaces and tabs and therefore represents a break
    /// opportunity rather than content.
    /// </summary>
    /// <param name="atom">The atom to test.</param>
    /// <returns><see langword="true" /> if the atom is empty or contains only horizontal whitespace; otherwise <see langword="false" />.</returns>
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
