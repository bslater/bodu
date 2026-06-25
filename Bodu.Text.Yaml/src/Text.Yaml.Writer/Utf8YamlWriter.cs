// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml.Writer;

/// <summary>
/// Provides a forward-only writer that emits a YAML document in block style to a UTF-8 buffer, in the manner of
/// <see cref="System.Text.Json.Utf8JsonWriter" />.
/// </summary>
/// <remarks>
/// <para>
/// The writer emits block mappings and sequences with configurable indentation, choosing the most compact safe scalar
/// presentation for each value (plain when unambiguous, otherwise double-quoted). Empty mappings and sequences are
/// emitted in the flow forms <c>{}</c> and <c>[]</c> so they round-trip as empty collections rather than null.
/// </para>
/// <para>
/// The writer is a <see langword="ref struct" /> and cannot be boxed, stored on the heap, or captured by a lambda.
/// </para>
/// </remarks>
public ref struct Utf8YamlWriter
{
    private readonly IBufferWriter<byte> _output;
    private readonly int _indentSize;
    private Frame[] _stack;
    private int _depth;
    private bool _rootWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlWriter" /> struct.
    /// </summary>
    /// <param name="output">The buffer to which UTF-8 YAML is written.</param>
    /// <exception cref="ArgumentNullException"><paramref name="output" /> is <see langword="null" />.</exception>
    public Utf8YamlWriter(IBufferWriter<byte> output)
        : this(output, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8YamlWriter" /> struct with options.
    /// </summary>
    /// <param name="output">The buffer to which UTF-8 YAML is written.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="output" /> is <see langword="null" />.</exception>
    public Utf8YamlWriter(IBufferWriter<byte> output, YamlWriterOptions options)
    {
        Bodu.ThrowHelper.ThrowIfNull(output);
        _output = output;
        _indentSize = options.EffectiveIndentSize;
        _stack = new Frame[8];
        _depth = 0;
        _rootWritten = false;
    }

    /// <summary>
    /// Begins a block mapping at the current value position.
    /// </summary>
    public void WriteStartMapping() => OpenContainer(isMapping: true);

    /// <summary>
    /// Ends the current block mapping.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no open mapping to close.</exception>
    public void WriteEndMapping() => CloseContainer(isMapping: true);

    /// <summary>
    /// Begins a block sequence at the current value position.
    /// </summary>
    public void WriteStartSequence() => OpenContainer(isMapping: false);

    /// <summary>
    /// Ends the current block sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no open sequence to close.</exception>
    public void WriteEndSequence() => CloseContainer(isMapping: false);

    /// <summary>
    /// Writes a mapping key. The next write supplies its value.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The current container is not a mapping, or a key is already pending.
    /// </exception>
    public void WritePropertyName(string name)
    {
        Bodu.ThrowHelper.ThrowIfNull(name);
        if (_depth == 0 || !_stack[_depth - 1].IsMapping)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterPropertyNameOutsideMapping);

        ref var frame = ref _stack[_depth - 1];
        if (frame.PendingKey is not null)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterPropertyNamePending);

        frame.PendingKey = FormatScalar(name);
    }

    /// <summary>
    /// Writes a string scalar value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public void WriteString(string value)
    {
        Bodu.ThrowHelper.ThrowIfNull(value);
        EmitScalar(FormatScalar(value));
    }

    /// <summary>
    /// Writes an integer scalar value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public void WriteInt64(long value) => EmitScalar(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Writes a floating-point scalar value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    public void WriteDouble(double value) => EmitScalar(FormatDouble(value));

    /// <summary>
    /// Writes a boolean scalar value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public void WriteBoolean(bool value) => EmitScalar(value ? "true" : "false");

    /// <summary>
    /// Writes a null scalar value.
    /// </summary>
    public void WriteNull() => EmitScalar("null");

    /// <summary>
    /// Opens a block container as the current value, deferring its lead until the first entry.
    /// </summary>
    /// <param name="isMapping">Whether the container is a mapping.</param>
    private void OpenContainer(bool isMapping)
    {
        Frame child;
        if (_depth == 0)
        {
            if (_rootWritten)
                throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterDocumentComplete);

            child = new Frame { IsMapping = isMapping, IsRoot = true, EntryIndent = 0 };
            _rootWritten = true;
        }
        else
        {
            ref var parent = ref _stack[_depth - 1];
            EnsureStarted(_depth - 1);
            child = new Frame
            {
                IsMapping = isMapping,
                LeadIndent = parent.EntryIndent,
                EntryIndent = parent.EntryIndent + _indentSize,
            };

            if (parent.IsMapping)
            {
                child.LeadKey = parent.PendingKey ?? throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterValueWithoutKey);
                parent.PendingKey = null;
            }
            else
            {
                child.LeadDash = true;
            }
        }

        Push(child);
    }

    /// <summary>
    /// Closes the current container, emitting an inline empty form when no entries were written.
    /// </summary>
    /// <param name="isMapping">Whether the expected container is a mapping.</param>
    private void CloseContainer(bool isMapping)
    {
        if (_depth == 0)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterNoOpenContainer);

        if (_stack[_depth - 1].IsMapping != isMapping)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterEndMismatch);

        if (_stack[_depth - 1].PendingKey is not null)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterPropertyNameWithoutValue);

        var frame = _stack[_depth - 1];
        _depth--;

        if (!frame.Started)
        {
            var empty = isMapping ? "{}" : "[]";
            if (frame.IsRoot)
            {
                WriteRaw(empty);
                WriteRaw("\n");
            }
            else if (frame.LeadKey is not null)
            {
                WriteIndent(frame.LeadIndent);
                WriteRaw(frame.LeadKey);
                WriteRaw(": ");
                WriteRaw(empty);
                WriteRaw("\n");
            }
            else
            {
                WriteIndent(frame.LeadIndent);
                WriteRaw("- ");
                WriteRaw(empty);
                WriteRaw("\n");
            }
        }
    }

    /// <summary>
    /// Emits a scalar value at the current value position, prefixing the mapping key or sequence dash.
    /// </summary>
    /// <param name="text">The already-formatted scalar literal.</param>
    private void EmitScalar(string text)
    {
        if (_depth == 0)
        {
            if (_rootWritten)
                throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterDocumentComplete);

            _rootWritten = true;
            WriteRaw(text);
            WriteRaw("\n");
            return;
        }

        EnsureStarted(_depth - 1);
        ref var frame = ref _stack[_depth - 1];
        if (frame.IsMapping)
        {
            var key = frame.PendingKey ?? throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_WriterValueWithoutKey);
            frame.PendingKey = null;
            WriteIndent(frame.EntryIndent);
            WriteRaw(key);
            WriteRaw(": ");
            WriteRaw(text);
            WriteRaw("\n");
        }
        else
        {
            WriteIndent(frame.EntryIndent);
            WriteRaw("- ");
            WriteRaw(text);
            WriteRaw("\n");
        }
    }

    /// <summary>
    /// Emits a container's deferred lead (<c>key:</c> or <c>-</c> on its own line) before its first entry.
    /// </summary>
    /// <param name="frameIndex">The stack index of the container.</param>
    private void EnsureStarted(int frameIndex)
    {
        ref var frame = ref _stack[frameIndex];
        if (frame.Started)
            return;

        frame.Started = true;
        if (frame.IsRoot)
            return;

        if (frame.LeadKey is not null)
        {
            WriteIndent(frame.LeadIndent);
            WriteRaw(frame.LeadKey);
            WriteRaw(":\n");
        }
        else if (frame.LeadDash)
        {
            WriteIndent(frame.LeadIndent);
            WriteRaw("-\n");
        }
    }

    /// <summary>
    /// Formats a string as a plain scalar when unambiguous, or as a double-quoted scalar otherwise.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The scalar literal to emit.</returns>
    /// <exception cref="ArgumentException"><paramref name="value" /> contains an unpaired surrogate.</exception>
    private static string FormatScalar(string value)
    {
        ValidateNoUnpairedSurrogates(value);
        return IsPlainSafe(value) ? value : DoubleQuote(value);
    }

    /// <summary>
    /// Validates that a string contains no unpaired surrogate, which could not be encoded as valid UTF-8.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentException"><paramref name="value" /> contains an unpaired surrogate.</exception>
    private static void ValidateNoUnpairedSurrogates(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw new ArgumentException(YamlResourceStrings.Arg_Invalid_YamlUnpairedSurrogate, nameof(value));

                i++;
            }
            else if (char.IsLowSurrogate(c))
            {
                throw new ArgumentException(YamlResourceStrings.Arg_Invalid_YamlUnpairedSurrogate, nameof(value));
            }
        }
    }

    /// <summary>
    /// Determines whether a string can be emitted as a plain scalar without changing its meaning.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns><see langword="true" /> when plain emission is safe.</returns>
    private static bool IsPlainSafe(string value)
    {
        if (value.Length == 0)
            return false;

        if (value != value.Trim())
            return false;

        var first = value[0];
        if (first is '-' or '?' or ':' or ',' or '[' or ']' or '{' or '}' or '#' or '&' or '*'
            or '!' or '|' or '>' or '\'' or '"' or '%' or '@' or '`' or ' ')
        {
            return false;
        }

        foreach (var c in value)
        {
            // Non-printable characters (C0 controls, DEL, and C1 controls) force quoting so they can be escaped.
            if (c < 0x20 || (c >= 0x7F && c <= 0x9F))
                return false;
        }

        if (value.Contains(": ", StringComparison.Ordinal) || value.EndsWith(':') ||
            value.Contains(" #", StringComparison.Ordinal))
        {
            return false;
        }

        // A plain scalar that would resolve to a non-string type must be quoted to preserve its string value.
        var bytes = Encoding.UTF8.GetBytes(value);
        return YamlScalarResolver.Resolve(bytes, YamlSpecVersion.V1_1, out _) == YamlValueKind.String;
    }

    /// <summary>
    /// Produces a double-quoted scalar literal with the necessary escapes.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The quoted literal.</returns>
    private static string DoubleQuote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                case '\0': sb.Append("\\0"); break;
                default:
                    if (c < 0x20 || (c >= 0x7F && c <= 0x9F))
                        sb.Append("\\x").Append(((int)c).ToString("x2", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>
    /// Formats a double in YAML canonical form, including the infinity and not-a-number spellings.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted literal.</returns>
    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value))
            return ".nan";

        if (double.IsPositiveInfinity(value))
            return ".inf";

        if (double.IsNegativeInfinity(value))
            return "-.inf";

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes the given number of indentation spaces.
    /// </summary>
    /// <param name="count">The number of spaces.</param>
    private void WriteIndent(int count)
    {
        if (count <= 0)
            return;

        var span = _output.GetSpan(count);
        span[..count].Fill((byte)' ');
        _output.Advance(count);
    }

    /// <summary>
    /// Writes a UTF-8 string to the output buffer.
    /// </summary>
    /// <param name="text">The text to write.</param>
    private readonly void WriteRaw(string text)
    {
        var count = Encoding.UTF8.GetByteCount(text);
        var span = _output.GetSpan(count);
        var written = Encoding.UTF8.GetBytes(text, span);
        _output.Advance(written);
    }

    /// <summary>
    /// Grows the traversal stack and pushes a frame.
    /// </summary>
    /// <param name="frame">The frame to push.</param>
    private void Push(Frame frame)
    {
        if (_depth == _stack.Length)
            Array.Resize(ref _stack, _stack.Length * 2);

        _stack[_depth] = frame;
        _depth++;
    }

    /// <summary>
    /// Tracks the emission state of an open container.
    /// </summary>
    private struct Frame
    {
        /// <summary>Whether the container is a mapping.</summary>
        public bool IsMapping;

        /// <summary>Whether the container is the document root.</summary>
        public bool IsRoot;

        /// <summary>Whether the container's lead has been emitted (or it has produced any entry).</summary>
        public bool Started;

        /// <summary>The indentation at which this container's entries are written.</summary>
        public int EntryIndent;

        /// <summary>The indentation at which this container's introducing lead is written.</summary>
        public int LeadIndent;

        /// <summary>The formatted key that introduces this container when its parent is a mapping.</summary>
        public string? LeadKey;

        /// <summary>Whether this container is introduced by a sequence dash.</summary>
        public bool LeadDash;

        /// <summary>For a mapping, the formatted key awaiting its value.</summary>
        public string? PendingKey;
    }
}
