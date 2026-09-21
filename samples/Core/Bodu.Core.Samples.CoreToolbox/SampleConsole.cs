// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SampleConsole.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Core.Samples.CoreToolbox;

/// <summary>
/// The console layout every scenario prints through: a titled banner carrying what the scenario does, why the
/// behaviour matters, and what a reader should expect to see, followed by the scenario's own labelled values.
/// </summary>
/// <remarks>
/// The banner exists so the transcript stands on its own. A reader who runs the sample should be able to tell a
/// correct run from a broken one without opening the source or reconstructing the algorithm from the hex.
/// </remarks>
public static class SampleConsole
{
    /// <summary>The column the wrapped narration text is folded at.</summary>
    private const int Width = 116;

    /// <summary>The width of the narration label column, including its separator.</summary>
    private const int LabelWidth = 11;

    /// <summary>
    /// Writes a scenario banner: the title, then the what/why/expect narration.
    /// </summary>
    /// <param name="title">The scenario title, shown between rule markers.</param>
    /// <param name="what">What the scenario computes, in one or two sentences.</param>
    /// <param name="why">Why the behaviour matters to a consumer of the API.</param>
    /// <param name="expect">The result a correct run prints, so a broken run is recognisable.</param>
    public static void Scenario(string title, string what, string why, string expect)
    {
        Console.WriteLine($"--- {title} ---");
        WriteNarration("What", what);
        WriteNarration("Why", why);
        WriteNarration("Expect", expect);
        Console.WriteLine();
    }

    /// <summary>
    /// Writes one labelled narration paragraph, wrapped and hanging-indented under its label.
    /// </summary>
    /// <param name="label">The narration label.</param>
    /// <param name="text">The paragraph to wrap.</param>
    private static void WriteNarration(string label, string text)
    {
        var indent = new string(' ', LabelWidth);
        var prefix = $"  {label,-6} : ";
        var line = new System.Text.StringBuilder(prefix);
        var column = prefix.Length;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (column > LabelWidth && column + word.Length + 1 > Width)
            {
                Console.WriteLine(line.ToString());
                line.Clear().Append(indent);
                column = indent.Length;
            }

            if (column > indent.Length) { line.Append(' '); column++; }

            line.Append(word);
            column += word.Length;
        }

        Console.WriteLine(line.ToString());
    }
}
