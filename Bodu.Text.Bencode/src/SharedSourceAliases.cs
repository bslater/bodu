// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedSourceAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

// Format aliases consumed by the shared serializer source in Bodu.Text.Serialization/shared/, which is
// compiled into this assembly (see the csproj Compile link). Each Bodu text-format package defines the same
// alias names against its own reader/writer/options/converter/exception types, so the shared files read as
// ordinary C# while binding to this format's ref-struct-bound surface.
global using FormatConverter = Bodu.Text.Bencode.Serialization.BencodeConverter;
global using FormatConverterFactory = Bodu.Text.Bencode.Serialization.BencodeConverterFactory;
global using FormatOptions = Bodu.Text.Bencode.BencodeSerializerOptions;
global using FormatReader = Bodu.Text.Bencode.Reader.Utf8BencodeReader;
global using FormatResourceStrings = Bodu.BencodeResourceStrings;
global using FormatSerializationException = Bodu.Text.Bencode.BencodeSerializationException;
global using FormatWriteStack = Bodu.Text.Bencode.Serialization.BencodeWriteStack;
global using FormatWriter = Bodu.Text.Bencode.Writer.Utf8BencodeWriter;
