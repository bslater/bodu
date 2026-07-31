// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Builds a minimal valid document for the validation-lint tests.
    /// </summary>
    /// <returns>A valid document builder.</returns>
    private static NotableDateDocumentBuilder ValidLintDocument() =>
        NotableDateDocumentBuilder.Create("demo.lint")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(1, 1)));

    /// <summary>
    /// Verifies that linting a valid document returns no diagnostics.
    /// </summary>
    [TestMethod]
    public void Validate_WhenDocumentIsValid_ShouldReturnNoDiagnostics()
    {
        IReadOnlyList<NotableDateValidationDiagnostic> diagnostics = ValidLintDocument().Validate();

        Assert.IsFalse(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error), "A valid document must lint clean.");
    }

    /// <summary>
    /// Verifies that a rule referencing an unknown algorithm key lints as a <c>BODU-CAL-ALGORITHM</c> error without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Validate_WhenRuleUsesUnknownAlgorithmKey_ShouldReportAlgorithmDiagnostic()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.lint.algorithm")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Algorithm("no-such-algorithm")));

        IReadOnlyList<NotableDateValidationDiagnostic> diagnostics = builder.Validate();

        Assert.IsTrue(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error && d.Code == "BODU-CAL-ALGORITHM"));
    }

    /// <summary>
    /// Verifies that a document too incomplete to serialize — a concept with no rules — lints as a
    /// <c>BODU-CAL-BUILDER-INCOMPLETE</c> error rather than throwing the serialization exception.
    /// </summary>
    [TestMethod]
    public void Validate_WhenConceptHasNoRules_ShouldReportBuilderIncompleteDiagnostic()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.lint.norules")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => { });

        IReadOnlyList<NotableDateValidationDiagnostic> diagnostics = builder.Validate();

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("BODU-CAL-BUILDER-INCOMPLETE", diagnostics[0].Code);
        Assert.AreEqual(NotableDateValidationSeverity.Error, diagnostics[0].Severity);
    }

    /// <summary>
    /// Verifies that <c>TryBuild</c> materializes a resolvable resource for a valid document.
    /// </summary>
    [TestMethod]
    public void TryBuild_WhenDocumentIsValid_ShouldReturnTrueWithResource()
    {
        bool built = ValidLintDocument().TryBuild(out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsTrue(built);
        Assert.IsNotNull(resource);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error));

        var resolved = new NotableDateService(resource!)
            .Resolve(new DateOnly(2026, 1, 1), "XX");
        Assert.HasCount(1, resolved);
    }

    /// <summary>
    /// Verifies that <c>TryBuild</c> reports an invalid document through diagnostics instead of throwing, returning no
    /// resource.
    /// </summary>
    [TestMethod]
    public void TryBuild_WhenDocumentFailsValidation_ShouldReturnFalseWithDiagnostics()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.lint.invalid")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Algorithm("no-such-algorithm")));

        bool built = builder.TryBuild(out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(built);
        Assert.IsNull(resource);
        Assert.IsTrue(diagnostics.Any(d => d.Code == "BODU-CAL-ALGORITHM"));
    }

    /// <summary>
    /// Verifies that a clean <c>Validate</c> result guarantees <c>Build</c> succeeds for the same document — the lint
    /// and the build share one validation pipeline.
    /// </summary>
    [TestMethod]
    public void Validate_WhenClean_ShouldGuaranteeBuildSucceeds()
    {
        NotableDateDocumentBuilder builder = ValidLintDocument();

        Assert.IsFalse(builder.Validate().Any(d => d.Severity == NotableDateValidationSeverity.Error));

        NotableDateResource resource = builder.Build();
        Assert.AreEqual("demo.lint", resource.ResourceId);
    }
}
