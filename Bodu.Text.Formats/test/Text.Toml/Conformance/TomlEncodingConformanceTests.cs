// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlEncodingConformanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

/// <summary>
/// Runs the vendored <c>toml-test</c> <c>invalid/encoding/*</c> cases, the byte-level half of the conformance suite that
/// the string-driven <see cref="TomlConformanceTests" /> corpus cannot carry (their ill-formed UTF-8, UTF-16, and
/// misplaced byte-order marks have no JSON-string representation). Each case is a raw byte sequence that must be rejected
/// when read as a stream under both the strict TOML v1.0.0 default and the v1.1.0 profile, since encoding validity does
/// not vary between versions. The cases are data-driven over a published vector table, so the tests are tagged
/// <see cref="TestCategories.Regression" />.
/// </summary>
[TestClass]
public sealed class TomlEncodingConformanceTests
{
    /// <summary>
    /// The options that select the TOML v1.1.0 profile.
    /// </summary>
    private static readonly TomlReaderOptions s_v11 = new() { SpecVersion = TomlSpecVersion.V1_1 };

    /// <summary>
    /// The <c>invalid/encoding/*</c> case names the TOML v1.0.0 manifest lists.
    /// </summary>
    private static readonly HashSet<string> s_manifestEncodingV10 = LoadEncodingManifest("toml-test-files-1.0.0.txt");

    /// <summary>
    /// The <c>invalid/encoding/*</c> case names the TOML v1.1.0 manifest lists.
    /// </summary>
    private static readonly HashSet<string> s_manifestEncodingV11 = LoadEncodingManifest("toml-test-files-1.1.0.txt");

    /// <summary>
    /// Provides every vendored encoding case as a name/bytes pair.
    /// </summary>
    /// <returns>One <see cref="EncodingCase" /> per vendored <c>encoding/*.toml</c> resource.</returns>
    public static IEnumerable<object[]> EncodingCases()
    {
        foreach (EncodingCase testCase in LoadEncodingCases())
            yield return [testCase];
    }

#pragma warning disable CA1062 // Encoding cases are statically constructed by the data source and are never null.

    /// <summary>
    /// Verifies that every encoding case is rejected with a <see cref="TomlFormatException" /> when read as a stream
    /// under the strict TOML v1.0.0 default profile.
    /// </summary>
    /// <param name="testCase">The encoding case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(EncodingCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenEncodingCaseUnderV10_ShouldThrowTomlFormatException(EncodingCase testCase)
    {
        using MemoryStream stream = new(testCase.Bytes);

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream), $"Encoding case '{testCase.Name}' should have been rejected under strict TOML v1.0.0.");
    }

    /// <summary>
    /// Verifies that every encoding case is rejected with a <see cref="TomlFormatException" /> when read as a stream
    /// under the TOML v1.1.0 profile.
    /// </summary>
    /// <param name="testCase">The encoding case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(EncodingCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenEncodingCaseUnderV11_ShouldThrowTomlFormatException(EncodingCase testCase)
    {
        using MemoryStream stream = new(testCase.Bytes);

        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(stream, s_v11), $"Encoding case '{testCase.Name}' should have been rejected under TOML v1.1.0.");
    }

#pragma warning restore CA1062

    /// <summary>
    /// Verifies that the vendored encoding resources and the manifests' <c>encoding/*</c> entries are in exact
    /// correspondence, so the byte-level half of the corpus cannot drift on re-vendor any more than the string half can.
    /// </summary>
    [TestMethod]
    public void Encoding_WhenLoaded_ShouldVendorEveryManifestEncodingCase()
    {
        var vendored = LoadEncodingCases().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var name in s_manifestEncodingV10.Concat(s_manifestEncodingV11))
            Assert.IsTrue(vendored.Contains(name), $"Manifest encoding case '{name}' is not vendored as a resource.");

        foreach (var name in vendored)
        {
            Assert.IsTrue(
                s_manifestEncodingV10.Contains(name) || s_manifestEncodingV11.Contains(name),
                $"Vendored encoding resource '{name}' is not listed in any manifest.");
        }
    }

    /// <summary>
    /// Loads the vendored <c>encoding/*.toml</c> resources as name/bytes pairs.
    /// </summary>
    /// <returns>One <see cref="EncodingCase" /> per embedded encoding resource.</returns>
    private static IEnumerable<EncodingCase> LoadEncodingCases()
    {
        const string marker = ".encoding.";

        Assembly assembly = typeof(TomlEncodingConformanceTests).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var markerIndex = resourceName.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0 || !resourceName.EndsWith(".toml", StringComparison.Ordinal))
                continue;

            var caseName = "encoding/" + resourceName[(markerIndex + marker.Length)..^".toml".Length];

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            yield return new EncodingCase(caseName, buffer.ToArray());
        }
    }

    /// <summary>
    /// Loads the <c>invalid/encoding/*</c> case names from the named embedded manifest.
    /// </summary>
    /// <param name="resourceSuffix">The trailing portion of the embedded manifest resource name.</param>
    /// <returns>The encoding case names the manifest lists.</returns>
    private static HashSet<string> LoadEncodingManifest(string resourceSuffix)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        Assembly assembly = typeof(TomlEncodingConformanceTests).Assembly;
        var resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(resourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded manifest resource '{resourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.StartsWith("invalid/encoding/", StringComparison.Ordinal) && line.EndsWith(".toml", StringComparison.Ordinal))
                names.Add(line["invalid/".Length..^".toml".Length]);
        }

        return names;
    }

    /// <summary>
    /// Represents a single encoding conformance case: a raw byte sequence and its toml-test name.
    /// </summary>
    /// <param name="Name">The case name from the corpus (its path under <c>invalid/</c>, without the extension).</param>
    /// <param name="Bytes">The raw document bytes.</param>
    public sealed record EncodingCase(string Name, byte[] Bytes) : IKat;
}
