// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollectionTests.RecordFrom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class PositionedDiagnosticCollectionTests
{
    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.RecordFrom" /> gives a declaration the position recorded
    /// for its source, which a later scope for the declaration then takes.
    /// </summary>
    [TestMethod]
    public void RecordFrom_WhenTheSourceHasAPosition_ShouldTakeIt()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);
        object source = new();
        object copy = new();
        PositionedDiagnosticCollection.Record(positioned, source, PositionedNode());

        PositionedDiagnosticCollection.RecordFrom(positioned, copy, source);
        using (PositionedDiagnosticCollection.EnterDeclaration(positioned, copy))
            positioned.Add(Diagnostic());

        Assert.AreEqual((2, 4), (inner.Single().LineNumber, inner.Single().LinePosition));
    }

    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.RecordFrom" /> gives a declaration whose source has no
    /// position the position of the innermost open scope.
    /// </summary>
    [TestMethod]
    public void RecordFrom_WhenTheSourceHasNoPosition_ShouldTakeTheInnermostScopes()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);
        object imported = new();

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
            PositionedDiagnosticCollection.RecordFrom(positioned, imported, null);

        using (PositionedDiagnosticCollection.EnterDeclaration(positioned, imported))
            positioned.Add(Diagnostic());

        Assert.AreEqual((2, 4), (inner.Single().LineNumber, inner.Single().LinePosition));
    }

    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.RecordFrom" /> leaves a declaration that already has a
    /// position at it.
    /// </summary>
    [TestMethod]
    public void RecordFrom_WhenTheDeclarationHasAPosition_ShouldKeepIt()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);
        object declaration = new();
        object other = new();
        PositionedDiagnosticCollection.Record(positioned, declaration, PositionedNode());

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
            PositionedDiagnosticCollection.RecordFrom(positioned, declaration, other);

        positioned.Add(Diagnostic());
        using (PositionedDiagnosticCollection.EnterDeclaration(positioned, declaration))
            positioned.Add(Diagnostic());

        Assert.AreEqual((2, 4), (inner[^1].LineNumber, inner[^1].LinePosition));
    }
}
