// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.Reasons.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Attributes every profile-unsupported corpus case to a specific, recognized rejection reason, so the single
/// <c>UnsupportedFeatureRejected</c> category is not an opaque bucket. The reason is derived from the rejection the
/// parser actually produces and asserted to be one the profile names deliberately.
/// </summary>
public partial class YamlTestCorpusTests
{
    /// <summary>
    /// The recognized rejection reasons for profile-unsupported cases. Each unsupported corpus case must map to exactly
    /// one of these, so the conformance signal stays specific rather than a single broad category.
    /// </summary>
    private static readonly IReadOnlyList<string> KnownUnsupportedReasons =
    [
        "UnsupportedNonCoreTag",
        "UnsupportedComplexKey",
        "UnsupportedAnchorOverride",
        "UnsupportedAliasResolution",
        "UnsupportedDuplicateKey",
        "UnsupportedTrailingContent",
        "UnsupportedNodeForm",
        "UnsupportedFlowConstruct",
        "UnsupportedEscape",
        "UnsupportedTabIndentation",
        "UnsupportedDirective",
        "UnsupportedConstruct",
    ];

    /// <summary>
    /// Verifies that every profile-unsupported case is rejected for a recognized, specific reason, with none falling
    /// into an unrecognized bucket.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(UnsupportedFeatureCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenUnsupportedFeatureCase_ShouldRejectForRecognizedReason(YamlTestVector kat)
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse(kat.Yaml);
        });

        var reason = DeriveUnsupportedReason(ex.Message);
        Assert.IsTrue(
            KnownUnsupportedReasons.Contains(reason),
            $"{kat.Id}: rejection reason '{reason}' is not a recognized profile reason (message: {ex.Message}).");
    }

    /// <summary>
    /// Maps a profile-rejection message to a specific reason category, so that profile-unsupported cases carry a
    /// meaningful conformance signal instead of one broad classification.
    /// </summary>
    /// <param name="message">The rejection message produced by the parser.</param>
    /// <returns>The recognized reason category, or <c>Unknown</c> when the message is not attributable.</returns>
    private static string DeriveUnsupportedReason(string message)
    {
        if (Contains(message, "tag is not valid"))
            return "UnsupportedNonCoreTag";
        if (Contains(message, "key must resolve to a scalar"))
            return "UnsupportedComplexKey";
        if (Contains(message, "anchor") && Contains(message, "already defined"))
            return "UnsupportedAnchorOverride";
        if (Contains(message, "does not refer to a defined anchor"))
            return "UnsupportedAliasResolution";
        if (Contains(message, "key is already defined"))
            return "UnsupportedDuplicateKey";
        if (Contains(message, "after the end of a document"))
            return "UnsupportedTrailingContent";
        if (Contains(message, "node value"))
            return "UnsupportedNodeForm";
        if (Contains(message, "flow collection"))
            return "UnsupportedFlowConstruct";
        if (Contains(message, "escape sequence"))
            return "UnsupportedEscape";
        if (Contains(message, "tab character cannot be used for indentation"))
            return "UnsupportedTabIndentation";
        if (Contains(message, "directive is not valid"))
            return "UnsupportedDirective";
        if (Contains(message, "Unexpected content"))
            return "UnsupportedConstruct";

        return "Unknown";
    }

    /// <summary>Determines whether a message contains the given fragment, ignoring case.</summary>
    /// <param name="message">The message to search.</param>
    /// <param name="fragment">The fragment to find.</param>
    /// <returns><see langword="true" /> when the fragment is present.</returns>
    private static bool Contains(string message, string fragment) =>
        message.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
