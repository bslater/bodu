// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Reader;
using SharpFuzz;

namespace Bodu.Text.Yaml.Fuzzing;

/// <summary>
/// SharpFuzz entry point for the Bodu YAML parser. For each fuzzer-provided input it drives the parser, document
/// accessors, multi-document stream, serializer binding, and reader token surface, swallowing only the exceptions the
/// library is contracted to throw on malformed input (<see cref="YamlFormatException" /> and, for the binding paths,
/// <see cref="YamlSerializationException" />). Any other exception, plus a round-trip invariant break, propagates so
/// libFuzzer records it as a finding.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the libFuzzer loop, optionally isolating a single target via the <c>FUZZ_TARGET</c> environment variable
    /// (<c>parse</c>, <c>stream</c>, <c>deser</c>, <c>reader</c>, or <c>roundtrip</c>).
    /// </summary>
    private static void Main()
    {
        var target = Environment.GetEnvironmentVariable("FUZZ_TARGET");
        Fuzzer.LibFuzzer.Run(data => Dispatch(target, data));
    }

    /// <summary>
    /// Dispatches one fuzzer input to the selected target, or to every target when none is isolated.
    /// </summary>
    /// <param name="target">The isolated target name, or <see langword="null" /> to run all targets.</param>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void Dispatch(string? target, ReadOnlySpan<byte> data)
    {
        switch (target)
        {
            case "parse": FuzzParse(data); break;
            case "stream": FuzzStream(data); break;
            case "deser": FuzzDeserialize(data); break;
            case "reader": FuzzReader(data); break;
            case "roundtrip": FuzzRoundTrip(data); break;
            default:
                FuzzParse(data);
                FuzzStream(data);
                FuzzDeserialize(data);
                FuzzReader(data);
                FuzzRoundTrip(data);
                break;
        }
    }

    /// <summary>
    /// Parses a single document and fully walks it, reaching the value accessors.
    /// </summary>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void FuzzParse(ReadOnlySpan<byte> data)
    {
        try
        {
            using var document = YamlDocument.Parse(data);
            Walk(document.RootElement);
        }
        catch (YamlFormatException)
        {
            // Contracted rejection of malformed input.
        }
    }

    /// <summary>
    /// Parses a multi-document stream and walks each document.
    /// </summary>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void FuzzStream(ReadOnlySpan<byte> data)
    {
        try
        {
            var documents = YamlDocument.ParseAllDocuments(data);
            foreach (var document in documents)
            {
                Walk(document.RootElement);
                document.Dispose();
            }
        }
        catch (YamlFormatException)
        {
            // Contracted rejection of malformed input.
        }
    }

    /// <summary>
    /// Binds the input to a loosely-typed object graph through the serializer and walks the graph.
    /// </summary>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void FuzzDeserialize(ReadOnlySpan<byte> data)
    {
        try
        {
            WalkObject(YamlSerializer.Deserialize<object>(data));
        }
        catch (YamlFormatException)
        {
            // Contracted rejection of malformed input.
        }
        catch (YamlSerializationException)
        {
            // Contracted binding failure.
        }
    }

    /// <summary>
    /// Drives the forward-only reader to completion, touching each token's value accessor.
    /// </summary>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void FuzzReader(ReadOnlySpan<byte> data)
    {
        try
        {
            var reader = new Utf8YamlReader(data);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case YamlTokenType.PropertyName:
                    case YamlTokenType.String:
                        _ = reader.GetString();
                        break;
                    case YamlTokenType.Integer:
                        _ = reader.GetInt64();
                        break;
                    case YamlTokenType.Float:
                        _ = reader.GetDouble();
                        break;
                    case YamlTokenType.Boolean:
                        _ = reader.GetBoolean();
                        break;
                }
            }
        }
        catch (YamlFormatException)
        {
            // Contracted rejection of malformed input.
        }
    }

    /// <summary>
    /// Asserts the round-trip invariant: a document that parses must re-serialize to YAML that reparses to an equal
    /// value graph. A re-emit that fails to reparse, or a graph mismatch, is a finding.
    /// </summary>
    /// <param name="data">The fuzzer-provided input bytes.</param>
    private static void FuzzRoundTrip(ReadOnlySpan<byte> data)
    {
        object? first;
        try
        {
            first = YamlSerializer.Deserialize<object>(data);
        }
        catch (YamlFormatException)
        {
            return;
        }
        catch (YamlSerializationException)
        {
            return;
        }

        string reemitted;
        try
        {
            reemitted = YamlSerializer.Serialize(first);
        }
        catch (YamlSerializationException)
        {
            // The serializer may legitimately refuse to emit some graphs (for example colliding dictionary keys).
            return;
        }

        object? second;
        try
        {
            second = YamlSerializer.Deserialize<object>(reemitted);
        }
        catch (Exception ex)
        {
            throw new FuzzInvariantException("re-emitted YAML failed to reparse", ex);
        }

        var firstJson = JsonSerializer.Serialize(first);
        var secondJson = JsonSerializer.Serialize(second);
        if (!string.Equals(firstJson, secondJson, StringComparison.Ordinal))
            throw new FuzzInvariantException($"round-trip value mismatch\n{firstJson}\n!=\n{secondJson}");
    }

    /// <summary>
    /// Recursively walks a parsed document, calling the value accessor matching each node's kind.
    /// </summary>
    /// <param name="element">The element to walk.</param>
    private static void Walk(YamlElement element)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Mapping:
                foreach (var property in element.EnumerateMapping())
                {
                    _ = property.Name;
                    Walk(property.Value);
                }

                break;

            case YamlValueKind.Sequence:
                var length = element.GetSequenceLength();
                for (var i = 0; i < length; i++)
                    Walk(element[i]);

                break;

            case YamlValueKind.String:
                _ = element.GetString();
                break;

            case YamlValueKind.Integer:
                _ = element.GetInt64();
                break;

            case YamlValueKind.Float:
                _ = element.GetDouble();
                break;

            case YamlValueKind.Boolean:
                _ = element.GetBoolean();
                break;
        }
    }

    /// <summary>
    /// Recursively walks a loosely-typed object graph produced by the serializer.
    /// </summary>
    /// <param name="value">The graph node to walk.</param>
    private static void WalkObject(object? value)
    {
        switch (value)
        {
            case IDictionary<string, object?> map:
                foreach (var entry in map)
                    WalkObject(entry.Value);

                break;

            case IEnumerable<object?> list:
                foreach (var item in list)
                    WalkObject(item);

                break;

            default:
                _ = value?.ToString();
                break;
        }
    }
}
