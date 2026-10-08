// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.Step.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one step of a <c>gen:</c> pipeline or an edit script: a name and its arguments, unevaluated.
    /// </summary>
    /// <param name="Name">The step name, such as <c>Stream</c>, <c>Fat</c> or <c>CreateStream</c>.</param>
    /// <param name="Args">The arguments as written, split at commas outside parentheses.</param>
    private sealed record Step(string Name, IReadOnlyList<string> Args)
    {
        /// <summary>
        /// Returns the step as it is written in a catalogue row.
        /// </summary>
        /// <returns>The name, followed by the arguments in parentheses when there are any.</returns>
        public override string ToString() =>
            Args.Count == 0 ? Name : $"{Name}({string.Join(',', Args)})";
    }
}
