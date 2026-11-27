// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollection.Scope.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

internal sealed partial class PositionedDiagnosticCollection
{
    /// <summary>
    /// Marks the examination of one node or declaration, and restores the enclosing position when it is disposed.
    /// </summary>
    /// <remarks>
    /// The default value belongs to no collection, and disposing it does nothing; it is what a collection that records
    /// no positions hands out.
    /// </remarks>
    internal readonly struct Scope
        : IDisposable
    {
        /// <summary>The collection whose position the scope restores, or <see langword="null" /> for none.</summary>
        private readonly PositionedDiagnosticCollection? _owner;

        /// <summary>The position that was in force when the scope opened.</summary>
        private readonly NotableDateSourcePosition? _enclosing;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope" /> struct.
        /// </summary>
        /// <param name="owner">The collection whose position the scope restores.</param>
        /// <param name="enclosing">The position that was in force when the scope opened.</param>
        public Scope(PositionedDiagnosticCollection owner, NotableDateSourcePosition? enclosing)
        {
            _owner = owner;
            _enclosing = enclosing;
        }

        /// <summary>
        /// Restores the position that was in force when the scope opened.
        /// </summary>
        public void Dispose()
        {
            if (_owner is not null)
                _owner._current = _enclosing;
        }
    }
}
