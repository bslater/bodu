// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteGovernance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Test.Corpus;

/// <summary>
/// Provides the checks an area's governance tests run over its release-note catalogues, beyond those
/// <see cref="ReleaseNoteCatalog.Load" /> makes of each file on its own.
/// </summary>
public static class ReleaseNoteGovernance
{
    /// <summary>
    /// Finds the <c>unit</c> rows whose named test method does not exist.
    /// </summary>
    /// <param name="rows">
    /// The rows to check; rows of other kinds, and <c>n/a</c> and <c>unknown</c> rows, are skipped.
    /// </param>
    /// <param name="testAssembly">The test assembly that should hold the named tests.</param>
    /// <returns>
    /// One message per <c>applies</c> or <c>dialect</c> unit row whose input does not name a method marked
    /// <see cref="TestMethodAttribute" /> on a type of that simple name in <paramref name="testAssembly" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="rows" /> or <paramref name="testAssembly" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<string> FindMissingUnitTests(IEnumerable<ReleaseNoteFix> rows, Assembly testAssembly)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(testAssembly);

        ILookup<string, Type> types = testAssembly.GetTypes().ToLookup(type => type.Name, StringComparer.Ordinal);
        var problems = new List<string>();

        foreach (ReleaseNoteFix row in rows)
        {
            if (row.Class is not ("applies" or "dialect") || row.Kind != "unit")
                continue;

            int dot = row.Input.IndexOf('.', StringComparison.Ordinal);
            string typeName = dot < 0 ? row.Input : row.Input[..dot];
            string methodName = dot < 0 ? string.Empty : row.Input[(dot + 1)..];

            bool found = types[typeName].Any(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(method => method.Name == methodName && method.IsDefined(typeof(TestMethodAttribute), inherit: true)));

            if (!found)
                problems.Add($"{row.FileName}:{row.LineNumber}: the unit test {row.Input} does not exist");
        }

        return problems;
    }

    /// <summary>
    /// Finds the rows whose options name an option the area does not recognize, or whose options are malformed.
    /// </summary>
    /// <param name="rows">The rows to check.</param>
    /// <param name="knownNames">The option names the area's corpus tests map.</param>
    /// <returns>One message per unrecognized name or malformed <c>options</c> field.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rows" /> or <paramref name="knownNames" /> is <see langword="null" />.</exception>
    public static IReadOnlyList<string> FindUnknownOptions(IEnumerable<ReleaseNoteFix> rows, IReadOnlyCollection<string> knownNames)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(knownNames);

        var problems = new List<string>();
        foreach (ReleaseNoteFix row in rows)
        {
            if (!ReleaseNoteOptions.TryParse(row.Options, out IReadOnlyList<KeyValuePair<string, string>>? options, out string? error))
            {
                problems.Add($"{row.FileName}:{row.LineNumber}: options: {error}");
                continue;
            }

            foreach (KeyValuePair<string, string> option in options)
            {
                if (!knownNames.Contains(option.Key, StringComparer.Ordinal))
                    problems.Add($"{row.FileName}:{row.LineNumber}: the option '{option.Key}' is not one of {string.Join(", ", knownNames)}");
            }
        }

        return problems;
    }
}
