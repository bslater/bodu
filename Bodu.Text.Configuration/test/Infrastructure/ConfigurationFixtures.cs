// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationFixtures.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration.Infrastructure;

/// <summary>
/// Provides canned <c>.boduconfig</c> fixtures used across the test suite to exercise the reader, writer,
/// resolver, and bridge.
/// </summary>
internal static class ConfigurationFixtures
{
    /// <summary>The simplest possible non-empty document — one section with one property.</summary>
    internal const string Minimal = """
[*]
format.indent.size = 4
""";

    /// <summary>A representative fixture with a preamble, two sections, and dotted keys.</summary>
    internal const string Representative = """
# Bodu configuration fixture
root = true

[*.cs]
format.indent.style = space
format.indent.size = 4
logging.level.default = Information

[src/**/*.{cs,csproj}]
format.indent.size = 2
logging.level.default = Warning
""";

    /// <summary>A fixture that exercises inline comments under the default Bodu profile.</summary>
    internal const string InlineComments = """
[*]
format.indent.size = 4 # standard Bodu indent
format.indent.style = space ; spaces, not tabs
""";

    /// <summary>A fixture demonstrating the EditorConfig <c>unset</c> sentinel.</summary>
    internal const string UnsetSentinel = """
[*]
format.indent.size = 4

[generated/**]
format.indent.size = unset
""";

    /// <summary>A fixture with leading comments preceding both sections and properties.</summary>
    internal const string CommentsAndProperties = """
# leading section comment
[*.cs]
# leading property comment
format.indent.size = 4
""";

    /// <summary>A fixture that exercises duplicate keys and last-wins precedence.</summary>
    internal const string DuplicateKeys = """
[*]
format.indent.size = 4
format.indent.size = 2
""";
}
