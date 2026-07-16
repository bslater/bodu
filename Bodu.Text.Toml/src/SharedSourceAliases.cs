// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedSourceAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

// Format aliases consumed by the shared serializer source in Bodu.Text.Serialization/shared/, which is
// compiled into this assembly (see the csproj Compile link). Each Bodu text-format package defines the same
// alias names against its own reader/writer/options/converter/exception types, so the shared files read as
// ordinary C# while binding to this format's ref-struct-bound surface.
global using FormatConverter = Bodu.Text.Toml.Serialization.TomlConverter;
global using FormatConverterFactory = Bodu.Text.Toml.Serialization.TomlConverterFactory;
global using FormatNode = Bodu.Text.Toml.Nodes.TomlNode;
global using FormatObject = Bodu.Text.Toml.Nodes.TomlObject;
global using FormatOptions = Bodu.Text.Toml.TomlSerializerOptions;
global using FormatReader = Bodu.Text.Toml.Reader.TomlDocumentReader;
global using FormatResourceStrings = Bodu.TomlResourceStrings;
global using FormatSerializationException = Bodu.Text.Toml.TomlSerializationException;
global using FormatWriteStack = Bodu.Text.Toml.Serialization.TomlWriteStack;
global using FormatWriter = Bodu.Text.Toml.Writer.Utf8TomlWriter;
