// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// The authoritative TOML structural parser: consumes the source-order token stream of a <see cref="Utf8TomlReader" />
/// and materializes the document into a flat <see cref="TomlReaderRow" /> store, enforcing the specification's key,
/// value, table, and array-of-tables rules for the configured <see cref="TomlSpecVersion" />.
/// </summary>
/// <remarks>
/// <para>
/// The lexer owns lexical validation; this type owns everything structural: dotted-key semantics, table reopening,
/// implicit-versus-explicit table tracking, arrays of tables, duplicate definitions, inline-table closedness, and the
/// maximum nesting depth. The split means a structurally invalid document lexes cleanly and is rejected here, with the
/// error position taken from the offending token.
/// </para>
/// <para>
/// TOML cannot be tokenized into tree order in a single forward pass, because out-of-line <c>[table]</c> and
/// <c>[[array-of-tables]]</c> headers contribute to structure defined elsewhere in the document. The builder therefore
/// appends rows in document order and links them by index, navigating to an already-recorded table to merge a later
/// out-of-line header into it. The finished row store (root at index 0) backs both random-access navigation in
/// <c>TomlDocument</c> and the depth-first cursor in <see cref="TomlDocumentReader" />.
/// </para>
/// </remarks>
internal sealed class TomlDocumentBuilder
{
    /// <summary>
    /// The flat row store, with the document root table at index 0.
    /// </summary>
    private readonly List<TomlReaderRow> _rows = [];

    /// <summary>
    /// Per-depth scratch lists reused to collect the segments of a key path without allocating a list per key. Indexed
    /// by the current nesting <see cref="_depth" />, so an outer key path is never overwritten by an inner path read
    /// while the outer value is being materialized.
    /// </summary>
    private readonly List<List<string>> _pathScratch = [];

    /// <summary>
    /// The TOML specification version whose grammar features the lexer enforces.
    /// </summary>
    private readonly TomlSpecVersion _specVersion;

    /// <summary>
    /// The maximum nesting depth of tables and arrays the builder will accept, clamped to
    /// <see cref="TomlLimits.AbsoluteMaxDepth" />.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The current nesting depth of arrays and inline tables, used to enforce <see cref="_maxDepth" />.
    /// </summary>
    private int _depth;

    /// <summary>
    /// The row index of the table that bare key/value pairs are currently assigned to.
    /// </summary>
    private int _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentBuilder" /> class.
    /// </summary>
    /// <param name="specVersion">The specification version whose grammar to enforce.</param>
    /// <param name="maxDepth">The maximum nesting depth of tables and arrays to accept.</param>
    /// <remarks>
    /// The effective depth is clamped to <see cref="TomlLimits.AbsoluteMaxDepth" /> so that an unbounded configured
    /// value cannot drive the parser into a <see cref="StackOverflowException" />.
    /// </remarks>
    internal TomlDocumentBuilder(TomlSpecVersion specVersion, int maxDepth)
    {
        _specVersion = specVersion;
        _maxDepth = Math.Min(maxDepth, TomlLimits.AbsoluteMaxDepth);
        _current = NewTable(0, 0);
    }

    /// <summary>
    /// Parses the supplied UTF-8 TOML source and returns the flat row store, with the root table at index 0.
    /// </summary>
    /// <param name="source">The UTF-8 TOML source bytes.</param>
    /// <returns>The flat row store describing the document.</returns>
    /// <exception cref="TomlFormatException">Thrown when the source is not valid TOML.</exception>
    /// <remarks>
    /// The store is returned without copying — the builder's own backing list becomes the document's store — and is
    /// pre-sized from the source length so it grows without repeated doubling for typical documents.
    /// </remarks>
    internal List<TomlReaderRow> Parse(ReadOnlySpan<byte> source)
    {
        _rows.EnsureCapacity(EstimateRowCapacity(source.Length));

        var lexer = new Utf8TomlReader(source, new TomlReaderOptions { SpecVersion = _specVersion, MaxDepth = _maxDepth });
        while (lexer.Read())
        {
            switch (lexer.TokenType)
            {
                case TomlTokenType.Comment:
                    break;

                case TomlTokenType.TableHeader:
                    DefineStandardTable(ReadHeaderPath(ref lexer), ref lexer);
                    break;

                case TomlTokenType.ArrayTableHeader:
                    DefineArrayTable(ReadHeaderPath(ref lexer), ref lexer);
                    break;

                case TomlTokenType.Key:
                default:
                {
                    List<string> path = ReadKeyPath(ref lexer);
                    var value = ReadValue(ref lexer);
                    AssignKeyValue(_current, path, value, ref lexer);
                    break;
                }
            }
        }

        return _rows;
    }

    /// <summary>
    /// Estimates the number of rows a document of the supplied byte length will produce, used to size the row store so
    /// it grows without repeated doubling for typical documents.
    /// </summary>
    /// <param name="sourceLength">The UTF-8 source length in bytes.</param>
    /// <returns>The initial row-store capacity.</returns>
    /// <remarks>
    /// Flat TOML produces roughly one row per dozen bytes of key/value text. The estimate is clamped to a ceiling so a
    /// large but sparse document — for example one dominated by long string values — cannot over-allocate the store;
    /// such a document simply grows it on demand instead.
    /// </remarks>
    private static int EstimateRowCapacity(int sourceLength) =>
        Math.Clamp(sourceLength / 12, 4, 4096);

    /// <summary>
    /// Reads the key segments of a header whose <see cref="TomlTokenType.TableHeader" /> or
    /// <see cref="TomlTokenType.ArrayTableHeader" /> token is current.
    /// </summary>
    /// <param name="lexer">The lexer to read from.</param>
    /// <returns>The key segments in order, held in the depth's reusable scratch list.</returns>
    private List<string> ReadHeaderPath(ref Utf8TomlReader lexer)
    {
        _ = lexer.Read();
        return ReadKeyPath(ref lexer);
    }

    /// <summary>
    /// Reads the remaining segments of a dotted key path whose first <see cref="TomlTokenType.Key" /> token is current.
    /// </summary>
    /// <param name="lexer">The lexer to read from.</param>
    /// <returns>The key segments in order, held in the depth's reusable scratch list.</returns>
    /// <remarks>
    /// The returned list is scratch reused for the next path read at the same depth, so the caller must consume it
    /// before reading another path at that depth. The key strings it holds are independent and remain valid once
    /// extracted.
    /// </remarks>
    private List<string> ReadKeyPath(ref Utf8TomlReader lexer)
    {
        List<string> keys = RentPath();
        keys.Add(lexer.GetString());
        while (!lexer.IsFinalKeySegment)
        {
            _ = lexer.Read();
            keys.Add(lexer.GetString());
        }

        return keys;
    }

    /// <summary>
    /// Returns a cleared scratch list for collecting the segments of a key path at the current nesting depth.
    /// </summary>
    /// <returns>A reusable, empty list scoped to <see cref="_depth" />.</returns>
    private List<string> RentPath()
    {
        while (_pathScratch.Count <= _depth)
            _pathScratch.Add([]);

        List<string> path = _pathScratch[_depth];
        path.Clear();
        return path;
    }

    /// <summary>
    /// Reads the next value from the lexer, materializing arrays and inline tables recursively.
    /// </summary>
    /// <param name="lexer">The lexer to read from, positioned before the value's first token.</param>
    /// <returns>The row index of the materialized value.</returns>
    private int ReadValue(ref Utf8TomlReader lexer)
    {
        _ = lexer.Read();
        return ReadValueAtCurrent(ref lexer);
    }

    /// <summary>
    /// Materializes the value whose first token is current: a scalar, an array, or an inline table.
    /// </summary>
    /// <param name="lexer">The lexer positioned on the value's first token.</param>
    /// <returns>The row index of the materialized value.</returns>
    private int ReadValueAtCurrent(ref Utf8TomlReader lexer)
    {
        switch (lexer.TokenType)
        {
            case TomlTokenType.StartArray:
            {
                EnterDepth(ref lexer);
                var array = NewArray(lexer.TokenStartIndex);
                while (true)
                {
                    _ = lexer.Read();
                    if (lexer.TokenType == TomlTokenType.Comment)
                        continue;
                    if (lexer.TokenType == TomlTokenType.EndArray)
                        break;

                    Link(array, ReadValueAtCurrent(ref lexer), null);
                }

                LeaveDepth();
                return array;
            }

            case TomlTokenType.StartInlineTable:
            {
                EnterDepth(ref lexer);
                var table = NewTable(lexer.TokenStartIndex, 0);
                while (true)
                {
                    _ = lexer.Read();
                    if (lexer.TokenType == TomlTokenType.Comment)
                        continue;
                    if (lexer.TokenType == TomlTokenType.EndInlineTable)
                        break;

                    List<string> path = ReadKeyPath(ref lexer);
                    var value = ReadValue(ref lexer);
                    AssignKeyValue(table, path, value, ref lexer);
                }

                AddFlag(table, TomlReaderRowFlags.Inline);
                LeaveDepth();
                return table;
            }

            case TomlTokenType.String:
                return NewScalar(TomlTokenType.String, lexer.TokenStartIndex, bits: TomlReaderRow.PackStringSpan(lexer.ValueStart, lexer.ValueLength, lexer.HasEscapes));

            case TomlTokenType.Integer:
                return NewScalar(TomlTokenType.Integer, lexer.TokenStartIndex, bits: lexer.GetInt64());

            case TomlTokenType.Float:
                return NewScalar(TomlTokenType.Float, lexer.TokenStartIndex, bits: BitConverter.DoubleToInt64Bits(lexer.GetDouble()));

            case TomlTokenType.Boolean:
                return NewScalar(TomlTokenType.Boolean, lexer.TokenStartIndex, bits: lexer.GetBoolean() ? 1L : 0L);

            case TomlTokenType.OffsetDateTime:
            {
                var dateTimeOffset = lexer.GetDateTimeOffset();
                return NewScalar(TomlTokenType.OffsetDateTime, lexer.TokenStartIndex, bits: dateTimeOffset.Ticks, offsetMinutes: (short)(dateTimeOffset.Offset.Ticks / TimeSpan.TicksPerMinute));
            }

            case TomlTokenType.LocalDateTime:
                return NewScalar(TomlTokenType.LocalDateTime, lexer.TokenStartIndex, bits: lexer.GetDateTime().Ticks);

            case TomlTokenType.LocalDate:
                return NewScalar(TomlTokenType.LocalDate, lexer.TokenStartIndex, bits: lexer.GetDateOnly().DayNumber);

            case TomlTokenType.LocalTime:
            default:
                return NewScalar(TomlTokenType.LocalTime, lexer.TokenStartIndex, bits: lexer.GetTimeOnly().Ticks);
        }
    }

    /// <summary>
    /// Records entry into a nested array or inline table and enforces the maximum nesting depth.
    /// </summary>
    /// <param name="lexer">The lexer whose current token supplies the error position.</param>
    /// <exception cref="TomlFormatException">Thrown when the nesting depth exceeds the configured maximum.</exception>
    private void EnterDepth(ref Utf8TomlReader lexer)
    {
        _depth++;
        if (_depth > _maxDepth)
            throw lexer.TokenError(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Format_Invalid_TomlNestingTooDeep, _maxDepth));
    }

    /// <summary>
    /// Records exit from a nested array or inline table.
    /// </summary>
    private void LeaveDepth() => _depth--;

    /// <summary>
    /// Defines a standard table at <paramref name="path" /> and makes it the current table.
    /// </summary>
    /// <param name="path">The table key path.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void DefineStandardTable(List<string> path, ref Utf8TomlReader lexer)
    {
        var table = 0;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkHeaderSegment(table, path[i], ref lexer);

        var key = path[^1];
        var existing = FindChild(table, key);
        if (existing >= 0)
        {
            if (_rows[existing].Kind != TomlReaderNodeKind.Table)
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);
            if (HasFlag(existing, TomlReaderRowFlags.Inline))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);
            if (HasFlag(existing, TomlReaderRowFlags.HeaderDefined) || HasFlag(existing, TomlReaderRowFlags.Dotted))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);

            RemoveFlag(existing, TomlReaderRowFlags.ImplicitSuper);
            AddFlag(existing, TomlReaderRowFlags.HeaderDefined);
            _current = existing;
            return;
        }

        var created = CreateChildTable(table, ref lexer);
        Link(table, created, key);
        AddFlag(created, TomlReaderRowFlags.HeaderDefined);
        _current = created;
    }

    /// <summary>
    /// Defines (or appends to) an array of tables at <paramref name="path" /> and makes the new element the current
    /// table.
    /// </summary>
    /// <param name="path">The array key path.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void DefineArrayTable(List<string> path, ref Utf8TomlReader lexer)
    {
        var table = 0;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkHeaderSegment(table, path[i], ref lexer);

        var key = path[^1];
        int array;
        var existing = FindChild(table, key);
        if (existing >= 0)
        {
            if (_rows[existing].Kind != TomlReaderNodeKind.Array || !HasFlag(existing, TomlReaderRowFlags.TableArray))
            {
                throw lexer.TokenError(_rows[existing].Kind == TomlReaderNodeKind.Array
                    ? TomlResourceStrings.Format_Invalid_TomlAppendToStaticArray
                    : TomlResourceStrings.Format_Invalid_TomlArrayTableConflict);
            }

            array = existing;
        }
        else
        {
            array = NewArray(lexer.TokenStartIndex);
            AddFlag(array, TomlReaderRowFlags.TableArray);
            Link(table, array, key);
        }

        var element = CreateChildTable(table, ref lexer);
        Link(array, element, null);
        _current = element;
    }

    /// <summary>
    /// Walks (creating if necessary) a single super-table segment of a table header path.
    /// </summary>
    /// <param name="table">The row index of the table to walk from.</param>
    /// <param name="key">The segment key.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The row index of the table for the segment.</returns>
    private int WalkHeaderSegment(int table, string key, ref Utf8TomlReader lexer)
    {
        if (HasFlag(table, TomlReaderRowFlags.Inline))
            throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);

        var existing = FindChild(table, key);
        if (existing >= 0)
        {
            if (_rows[existing].Kind == TomlReaderNodeKind.Table)
            {
                if (HasFlag(existing, TomlReaderRowFlags.Inline))
                    throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);

                return existing;
            }

            if (_rows[existing].Kind == TomlReaderNodeKind.Array && HasFlag(existing, TomlReaderRowFlags.TableArray) && _rows[existing].ChildCount > 0)
                return _rows[existing].LastChild;

            throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);
        }

        var created = CreateChildTable(table, ref lexer);
        Link(table, created, key);
        AddFlag(created, TomlReaderRowFlags.ImplicitSuper);
        return created;
    }

    /// <summary>
    /// Creates a child table one level beneath <paramref name="parent" />, enforcing the maximum nesting depth.
    /// </summary>
    /// <param name="parent">The row index of the table the child will be added under.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The row index of the created table.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when the child would exceed the configured maximum depth.
    /// </exception>
    private int CreateChildTable(int parent, ref Utf8TomlReader lexer)
    {
        if (_rows[parent].Depth >= _maxDepth)
            throw lexer.TokenError(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Format_Invalid_TomlNestingTooDeep, _maxDepth));

        return NewTable(lexer.TokenStartIndex, _rows[parent].Depth + 1);
    }

    /// <summary>
    /// Assigns a key/value pair under <paramref name="target" />, creating intermediate dotted-key tables.
    /// </summary>
    /// <param name="target">The row index of the table receiving the assignment.</param>
    /// <param name="path">The dotted key path.</param>
    /// <param name="value">The row index of the value to assign.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void AssignKeyValue(int target, List<string> path, int value, ref Utf8TomlReader lexer)
    {
        var table = target;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkDottedSegment(table, path[i], ref lexer);

        var key = path[^1];
        if (FindChild(table, key) >= 0)
            throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateKey);

        Link(table, value, key);
    }

    /// <summary>
    /// Walks (creating if necessary) a single intermediate table segment of a dotted key.
    /// </summary>
    /// <param name="table">The row index of the table to walk from.</param>
    /// <param name="key">The segment key.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The row index of the intermediate table.</returns>
    private int WalkDottedSegment(int table, string key, ref Utf8TomlReader lexer)
    {
        var existing = FindChild(table, key);
        if (existing >= 0)
        {
            if (_rows[existing].Kind != TomlReaderNodeKind.Table)
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlKeyOnValue);
            if (HasFlag(existing, TomlReaderRowFlags.Inline))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);

            // Dotted keys may not extend a table already defined in [table] form (see toml-lang/toml#846).
            if (HasFlag(existing, TomlReaderRowFlags.HeaderDefined))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);

            // Traversing an implicit super-table with a dotted key defines it via dotted keys: a later [table] header
            // may no longer re-open it, mirroring the rule that headers cannot redefine dotted-key tables.
            if (TryRemoveFlag(existing, TomlReaderRowFlags.ImplicitSuper))
                AddFlag(existing, TomlReaderRowFlags.Dotted);

            return existing;
        }

        var created = CreateChildTable(table, ref lexer);
        AddFlag(created, TomlReaderRowFlags.Dotted);
        Link(table, created, key);
        return created;
    }

    /// <summary>
    /// Appends a table row to the store and returns its index.
    /// </summary>
    /// <param name="offset">The source byte offset at which the table begins.</param>
    /// <param name="depth">The nesting depth of the table within its tree.</param>
    /// <returns>The row index of the new table.</returns>
    private int NewTable(int offset, int depth)
    {
        _rows.Add(new TomlReaderRow
        {
            Kind = TomlReaderNodeKind.Table,
            Offset = offset,
            Depth = depth,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
        });
        return _rows.Count - 1;
    }

    /// <summary>
    /// Appends an array row to the store and returns its index.
    /// </summary>
    /// <param name="offset">The source byte offset at which the array begins.</param>
    /// <returns>The row index of the new array.</returns>
    private int NewArray(int offset)
    {
        _rows.Add(new TomlReaderRow
        {
            Kind = TomlReaderNodeKind.Array,
            Offset = offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
        });
        return _rows.Count - 1;
    }

    /// <summary>
    /// Appends a scalar row to the store and returns its index.
    /// </summary>
    /// <param name="tokenType">The token type that classifies the scalar.</param>
    /// <param name="offset">The source byte offset at which the scalar begins.</param>
    /// <param name="bits">The packed payload: a value-type scalar's value, or a string scalar's content span.</param>
    /// <param name="offsetMinutes">The UTC offset in whole minutes for an offset date-time; otherwise zero.</param>
    /// <returns>The row index of the new scalar.</returns>
    private int NewScalar(TomlTokenType tokenType, int offset, long bits = 0L, short offsetMinutes = 0)
    {
        _rows.Add(new TomlReaderRow
        {
            Kind = TomlReaderNodeKind.Scalar,
            TokenType = tokenType,
            ScalarBits = bits,
            ScalarOffsetMinutes = offsetMinutes,
            Offset = offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
        });
        return _rows.Count - 1;
    }

    /// <summary>
    /// Appends <paramref name="child" /> to the child list of <paramref name="parent" /> under the supplied key.
    /// </summary>
    /// <param name="parent">The row index of the parent container.</param>
    /// <param name="child">The row index of the child to append.</param>
    /// <param name="key">The key for the child within a table, or <see langword="null" /> for an array element.</param>
    private void Link(int parent, int child, string? key)
    {
        TomlReaderRow c = _rows[child];
        c.Key = key;
        c.NextSibling = -1;
        _rows[child] = c;

        TomlReaderRow p = _rows[parent];
        if (p.FirstChild < 0)
        {
            p.FirstChild = child;
        }
        else
        {
            TomlReaderRow last = _rows[p.LastChild];
            last.NextSibling = child;
            _rows[p.LastChild] = last;
        }

        p.LastChild = child;
        p.ChildCount++;
        _rows[parent] = p;
    }

    /// <summary>
    /// Finds the child of <paramref name="table" /> with the supplied key.
    /// </summary>
    /// <param name="table">The row index of the table to search.</param>
    /// <param name="key">The key to find.</param>
    /// <returns>The row index of the matching child, or <c>-1</c> when none matches.</returns>
    private int FindChild(int table, string key)
    {
        var child = _rows[table].FirstChild;
        while (child >= 0)
        {
            if (string.Equals(_rows[child].Key, key, StringComparison.Ordinal))
                return child;

            child = _rows[child].NextSibling;
        }

        return -1;
    }

    /// <summary>
    /// Indicates whether the row at the supplied index carries the supplied structural flag.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <param name="flag">The flag to test.</param>
    /// <returns><see langword="true" /> when the flag is set; otherwise <see langword="false" />.</returns>
    private bool HasFlag(int index, TomlReaderRowFlags flag) =>
        (_rows[index].Flags & flag) != 0;

    /// <summary>
    /// Sets the supplied structural flag on the row at the supplied index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <param name="flag">The flag to set.</param>
    private void AddFlag(int index, TomlReaderRowFlags flag)
    {
        TomlReaderRow row = _rows[index];
        row.Flags |= flag;
        _rows[index] = row;
    }

    /// <summary>
    /// Clears the supplied structural flag on the row at the supplied index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <param name="flag">The flag to clear.</param>
    private void RemoveFlag(int index, TomlReaderRowFlags flag)
    {
        TomlReaderRow row = _rows[index];
        row.Flags &= ~flag;
        _rows[index] = row;
    }

    /// <summary>
    /// Clears the supplied flag and reports whether it had been set.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <param name="flag">The flag to clear.</param>
    /// <returns><see langword="true" /> when the flag had been set; otherwise <see langword="false" />.</returns>
    private bool TryRemoveFlag(int index, TomlReaderRowFlags flag)
    {
        if (!HasFlag(index, flag))
            return false;

        RemoveFlag(index, flag);
        return true;
    }
}
