// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollectionTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class PositionedDiagnosticCollectionTests
{
    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.Add" /> adds a diagnostic unchanged outside every scope.
    /// </summary>
    [TestMethod]
    public void Add_WhenOutsideEveryScope_ShouldAddTheDiagnosticUnchanged()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);

        positioned.Add(Diagnostic());

        Assert.AreEqual(Diagnostic(), inner.Single());
    }

    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.Add" /> gives a diagnostic added inside a scope the
    /// position of the scope's node.
    /// </summary>
    [TestMethod]
    public void Add_WhenInsideAScope_ShouldGiveTheDiagnosticTheNodesPosition()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
            positioned.Add(Diagnostic());

        Assert.AreEqual(Diagnostic() with { LineNumber = 2, LinePosition = 4 }, inner.Single());
    }

    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.Add" /> keeps the position a diagnostic carries of its
    /// own inside a scope.
    /// </summary>
    [TestMethod]
    public void Add_WhenTheDiagnosticCarriesAPosition_ShouldKeepIt()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);
        NotableDateValidationDiagnostic located = Diagnostic() with { LineNumber = 9, LinePosition = 1 };

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
            positioned.Add(located);

        Assert.AreEqual(located, inner.Single());
    }
}
