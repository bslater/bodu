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
/// The input is a list of strings produced by <see cref="DocLayout" />: alternating word atoms and single-space
/// whitespace separators. The wrapper treats whitespace atoms as break opportunities and word atoms as
/// indivisible. When a single word atom exceeds the column budget the wrapper emits it on its own line and
/// allows the line to exceed the budget rather than corrupt the content.
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

        StringBuilder current = new StringBuilder();
        bool pendingSpace = false;

        foreach (string atom in atoms)
        {
            if (IsWhitespaceAtom(atom))
            {
                if (current.Length > 0)
                {
                    pendingSpace = true;
                }

                continue;
            }

            int projected = current.Length + (pendingSpace ? 1 : 0) + atom.Length;
            if (current.Length == 0 || projected <= contentBudget)
            {
                if (pendingSpace)
                {
                    current.Append(' ');
                    pendingSpace = false;
                }

                current.Append(atom);
            }
            else
            {
                yield return current.ToString();
                current.Clear();
                pendingSpace = false;
                current.Append(atom);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
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
}
