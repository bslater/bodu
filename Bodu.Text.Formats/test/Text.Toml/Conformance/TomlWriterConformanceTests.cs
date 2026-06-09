// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterConformanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json.Nodes;

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

/// <summary>
/// Drives the writer against the vendored <c>toml-test</c> v1.0.0 valid corpus to prove that <see cref="TomlWriter" />
/// emits canonical output which (a) reparses under the strict v1.0.0 default — so it never emits a v1.1-only construct —
/// and (b) preserves the document's semantics, matching the same expected tagged-JSON value tree the reader is held to.
/// The cases are reused from <see cref="TomlConformanceTests.ValidCasesV10" />, so writer and reader conformance share a
/// single manifest-gated source, and the suite is tagged <see cref="TestCategories.Regression" /> accordingly.
/// </summary>
[TestClass]
public sealed class TomlWriterConformanceTests
{
#pragma warning disable CA1062 // Conformance cases are statically constructed by the data source and are never null.

    /// <summary>
    /// Verifies that writing a valid v1.0.0 document and reparsing the output under the strict v1.0.0 default yields a
    /// model semantically equivalent to the expected value tree — exercising the writer's strict-v1.0 emission and
    /// semantic round-trip across every scalar kind, nested table, array of tables, and inline table in the corpus.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(TomlConformanceTests.ValidCasesV10), typeof(TomlConformanceTests), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenValidV10Document_ShouldEmitStrictV10ThatPreservesModel(TomlConformanceTests.ConformanceCase testCase)
    {
        var model = Toml.Parse(testCase.Toml);
        var written = Toml.Format(model);

        // Reparsing under the strict v1.0.0 default proves the writer emitted no v1.1-only syntax.
        var reparsed = Toml.Parse(written);

        JsonNode actual = TomlTestEncoder.Encode(reparsed);
        JsonNode expected = JsonNode.Parse(testCase.Expected!)!;

        TaggedJson.AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
    }
}
