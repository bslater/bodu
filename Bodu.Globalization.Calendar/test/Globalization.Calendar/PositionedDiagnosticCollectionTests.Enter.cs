// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollectionTests.Enter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class PositionedDiagnosticCollectionTests
{
    /// <summary>
    /// Verifies that disposing a scope from <see cref="PositionedDiagnosticCollection.Enter" /> restores the position
    /// that was in force before it opened.
    /// </summary>
    [TestMethod]
    public void Enter_WhenTheScopeIsDisposed_ShouldRestoreTheEnclosingPosition()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
        {
        }

        positioned.Add(Diagnostic());

        Assert.IsNull(inner.Single().LineNumber);
    }

    /// <summary>
    /// Verifies that a scope from <see cref="PositionedDiagnosticCollection.Enter" /> for a node without a position
    /// leaves the enclosing scope's position in force.
    /// </summary>
    [TestMethod]
    public void Enter_WhenTheNodeHasNoPosition_ShouldKeepTheEnclosingPosition()
    {
        List<NotableDateValidationDiagnostic> inner = new();
        var positioned = new PositionedDiagnosticCollection(inner);

        using (PositionedDiagnosticCollection.Enter(positioned, PositionedNode()))
        using (PositionedDiagnosticCollection.Enter(positioned, UnpositionedNode()))
            positioned.Add(Diagnostic());

        Assert.AreEqual((2, 4), (inner.Single().LineNumber, inner.Single().LinePosition));
    }

    /// <summary>
    /// Verifies that <see cref="PositionedDiagnosticCollection.Enter" /> over a collection that records no positions
    /// opens a scope that leaves its diagnostics unchanged.
    /// </summary>
    [TestMethod]
    public void Enter_WhenTheCollectionRecordsNoPositions_ShouldLeaveItsDiagnosticsUnchanged()
    {
        List<NotableDateValidationDiagnostic> plain = new();

        using (PositionedDiagnosticCollection.Enter(plain, PositionedNode()))
            plain.Add(Diagnostic());

        Assert.AreEqual(Diagnostic(), plain.Single());
    }
}
