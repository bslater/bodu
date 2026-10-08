// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with: the profile whose presets parse, resolve and write the input,
    /// and the path the document is resolved for.
    /// </summary>
    /// <param name="Profile">The profile passed to the <c>For</c> method of each options type.</param>
    /// <param name="Target">The target path passed to <c>Resolve</c>.</param>
    private sealed record RowOptions(ConfigurationProfile Profile, string Target)
    {
        /// <summary>The target path a row resolves when its options name none.</summary>
        private const string DefaultTarget = "a.txt";

        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>
        /// The row options: the <see cref="ConfigurationProfile.Bodu" /> profile and the target <c>a.txt</c> unless the
        /// field names others.
        /// </returns>
        /// <exception cref="FormatException">
        /// An option is malformed, unknown, or has a value it does not accept.
        /// </exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(ConfigurationProfile.Bodu, DefaultTarget);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "Profile" => options with { Profile = ParseProfile(value) },
                    "Target" when value.Length > 0 => options with { Target = value },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Reads a <c>Profile</c> option value.
        /// </summary>
        /// <param name="value">The option value.</param>
        /// <returns>The profile the value names.</returns>
        /// <exception cref="FormatException">The value names no profile.</exception>
        private static ConfigurationProfile ParseProfile(string value) =>
            value switch
            {
                "Bodu" => ConfigurationProfile.Bodu,
                "EditorConfigCompatible" => ConfigurationProfile.EditorConfigCompatible,
                "Strict" => ConfigurationProfile.Strict,
                "Relaxed" => ConfigurationProfile.Relaxed,
                _ => throw new FormatException(
                    $"The option Profile takes Bodu, EditorConfigCompatible, Strict or Relaxed, not '{value}'."),
            };
    }
}
