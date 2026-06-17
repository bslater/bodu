// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzerTests.SupportedDiagnostics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Test;

public partial class XmlDocFormatAnalyzerTests
{
    /// <summary>
    /// Verifies that <see cref="XmlDocFormatAnalyzer" /> advertises only the formatting diagnostics it actually
    /// emits — the per-tag formatting IDs and the cross-cutting ID — and not the content-quality
    /// (<c>BODU1405</c>/<c>BODU1406</c>) or configuration (<c>BODU0001</c>) diagnostics owned by other analyzers.
    /// </summary>
    [TestMethod]
    public void SupportedDiagnostics_ShouldContainOnlyFormattingDescriptors()
    {
        ImmutableHashSet<string> ids = new XmlDocFormatAnalyzer().SupportedDiagnostics
            .Select(d => d.Id)
            .ToImmutableHashSet();

        Assert.IsTrue(ids.Contains("BODU1001"), "Expected the per-tag <summary> formatting diagnostic.");
        Assert.IsTrue(ids.Contains("BODU1018"), "Expected the per-tag <typeparamref> formatting diagnostic.");
        Assert.IsTrue(ids.Contains("BODU1040"), "Expected the cross-cutting formatting diagnostic.");

        Assert.IsFalse(ids.Contains("BODU1405"), "BODU1405 is emitted by XmlDocCodeRequiresCDataAnalyzer.");
        Assert.IsFalse(ids.Contains("BODU1406"), "BODU1406 is emitted by XmlDocTypeParamRequiresShortContentAnalyzer.");
        Assert.IsFalse(ids.Contains("BODU0001"), "BODU0001 is emitted by XmlDocConfigurationAnalyzer.");
    }
}
